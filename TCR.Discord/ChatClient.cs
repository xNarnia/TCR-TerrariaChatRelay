using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCRCore.Clients;
using TCRCore.Helpers;
using TCRCore.Clients.Discord;
using TCRDiscord.Helpers;
using TCRCore;
using TCRCore.Command;
using Discord.WebSocket;
using Discord;
using System.Timers;

namespace TCRDiscord
{
    public class ChatClient : BaseClient
    {
        public override string Name { get; set; } = "Discord";
        public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        public const string API_URL = "https://discordapp.com/api/v6";

        // Discord Variables
        public List<ulong> Channel_IDs { get; set; }
        public List<ulong> SendOnlyChannel_IDs { get; set; }
        public List<ulong> ReceiveOnlyChannel_IDs { get; set; }
        public Endpoint Endpoint { get; set; }
        public bool Reconnect { get; set; } = false;
        private string BOT_TOKEN { get; set; }
        private ChatParser chatParser { get; set; }

        // Message Queue
        private DiscordMessageQueue messageQueue { get; set; }

        // TCR Variables
        private List<IChatClient> parent { get; set; }
        public DiscordSocketClient Socket;
        private int errorCounter;
        private static int fatalErrorCounter;
        private bool retryConnection = false;
        private bool manualDisconnect = false;
        private Timer statusTimer;

        // Other
        private bool debug = false;

        public ChatClient(List<IChatClient> _parent, Endpoint _endpoint)
            : base(_parent)
        {
            parent = _parent;
            BOT_TOKEN = _endpoint.BotToken;
            Channel_IDs = _endpoint.Channel_IDs.ToList();
            Endpoint = _endpoint;
            chatParser = new ChatParser();

            statusTimer = new Timer(12000);
            statusTimer.Elapsed += GameStatusUpdate;

            messageQueue = new DiscordMessageQueue(500);
            messageQueue.OnReadyToSend += OnMessageReadyToSend;
        }

        /// <summary>
        /// Event fired when a message from in-game is received.
        /// Queues messages to stack messages closely sent to each other.
        /// This will allow TCR to combine messages and reduce messages sent to Discord.
        /// </summary>
        public void OnMessageReadyToSend(Dictionary<ulong, Queue<string>> messages)
        {
            foreach (var queue in messages)
            {
                string output = "";

                foreach (var msg in queue.Value)
                {
                    output += msg + '\n';
                }

                if (output.Length > 2000)
                    output = output.Substring(0, 2000);

                SendMessageToClient(output, queue.Key.ToString());
            }
        }

        /// <summary>
        /// Create a new WebSocket and initiate connection with Discord servers. 
        /// Utilizes BOT_TOKEN and CHANNEL_ID found in Mod Config.
        /// </summary>
        public override async void ConnectAsync()
        {
            if ((BOT_TOKEN == "BOT_TOKEN" || Channel_IDs.Contains(0)) && Reconnect == false)
            {
                PrettyPrint.Log("Please update your Mod Config. Mod reload required.");

                if (BOT_TOKEN == "BOT_TOKEN")
                    PrettyPrint.Log(" Invalid Token: BOT_TOKEN", ConsoleColor.Yellow);
                if (Channel_IDs.Contains(0))
                    PrettyPrint.Log(" Invalid Channel Id: 0", ConsoleColor.Yellow);

                PrettyPrint.Log("Config path: " + new Configuration().FileName);
                Console.ResetColor();
                Dispose();
                return;
            }

            if (Main.Config.OwnerUserId == 0 && Reconnect == false)
                PrettyPrint.Log(" Invalid Owner Id: 0", ConsoleColor.Yellow);

            errorCounter = 0;

            Socket = new DiscordSocketClient(new DiscordSocketConfig()
			{
                GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged,
                MessageCacheSize = 30,
                LogLevel = LogSeverity.Verbose
			});
            await Socket.LoginAsync(TokenType.Bot, BOT_TOKEN);
            await Socket.StartAsync();
            Socket.MessageReceived += DiscordMessageReceived;
			Socket.Connected += ConnectionSuccessful;
            Socket.Disconnected += ScheduleRetry;
        }

		private Task ConnectionSuccessful()
		{
            PrettyPrint.Log("Connection Established!", ConsoleColor.Green);
            errorCounter = 0;
            fatalErrorCounter = 0;
            retryConnection = false;
            Core.OnClientMessageReceived += Core_OnClientMessageReceived;
            Socket.Connected -= ConnectionSuccessful;
            statusTimer.Start();

			if (Main.Config.ShowPoweredByMessageOnStartup && !Reconnect)
			{
				messageQueue.QueueMessage(Channel_IDs,
					$"**This bot is powered by TerrariaChatRelay**\nUse **{Main.Config.CommandPrefix}help** for more commands!");
				Main.Config.ShowPoweredByMessageOnStartup = true;
				Main.Config.SaveJson();
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Unsubscribes all WebSocket events, then releases all resources used by the WebSocket.
		/// </summary>
		public override void Disconnect()
        {
            manualDisconnect = true;
            Core.OnClientMessageReceived -= Core_OnClientMessageReceived;

            // Detach queue from event and dispose
            if (messageQueue != null)
			{
                messageQueue.OnReadyToSend -= OnMessageReadyToSend;
                messageQueue.Clear();
            }
            messageQueue = null;

            if (statusTimer != null)
			{
                statusTimer.Stop();
                statusTimer.Dispose();
            }
            statusTimer = null;

            // Detach events
            if (Socket != null)
			{
                Socket.MessageReceived -= DiscordMessageReceived;
                Socket.StopAsync().GetAwaiter();
            }

            Socket = null;
        }

        /// <summary>
        /// Parses data when Discord sends a message.
        /// </summary>
        private Task DiscordMessageReceived(SocketMessage msg)
        {
            try
            {
                if (msg.Content == null 
                    || msg.Content.Length <= 1) 
                    return Task.CompletedTask;

                if (debug)
                    Console.WriteLine("\n" + msg.Content + "\n");

                if (msg != null && msg.Content != "" && Channel_IDs.Contains(msg.Channel.Id))
                {
                    if (!msg.Author.IsBot)
                    {
                        string msgout = msg.Content;
                        bool command = Core.CommandServ.IsCommand(msgout, Main.Config.CommandPrefix);

                        if (!command)
                        {
                            if (Endpoint.DenySendingMessagesToGame.Contains(msg.Channel.Id))
                                return Task.CompletedTask;
                            
                            msgout = chatParser.ConvertUserIdsToNames(msgout, msg.MentionedUsers);
                            msgout = chatParser.ShortenEmojisToName(msgout);
                        }

                        Permission userPermission;
                        if (msg.Author.Id == Main.Config.OwnerUserId)
                            userPermission = Permission.Owner;
                        else if (Main.Config.AdminUserIds.Contains(msg.Author.Id))
                            userPermission = Permission.Admin;
                        else if (Main.Config.ManagerUserIds.Contains(msg.Author.Id))
                            userPermission = Permission.Manager;
                        else
                            userPermission = Permission.User;

                        var user = new TCRClientUser(this.Name, msg.Author.Username, userPermission);

                        // There needs to be a better way of doing this rather than making exceptions that couple to commands
						if (command)
						{
                            string[] channelCommands = { "denysend", "denyreceive", "allowsend", "allowreceive" };

                            foreach(var commandKey in channelCommands)
							{
                                if(msgout.StartsWith(Main.Config.CommandPrefix + commandKey))
								{
                                    msgout = Main.Config.CommandPrefix + commandKey + " " + msg.Channel.Id;
								}
							}
                        }

                        TCRCore.Core.RaiseClientMessageReceived(this, user, this.Name, Configuration.TerrariaInGameDiscordPrefix, msgout, Main.Config.CommandPrefix, msg.Channel.Id.ToString());

                        msgout = $"<{msg.Author.Username}> {msgout}";

                        if (Channel_IDs.Count > 1 && !command)
                        {
                            messageQueue.QueueMessage(
                                Channel_IDs.Where(x => x != msg.Channel.Id && !Endpoint.DenyReceivingMessagesFromGame.Contains(x)),
                                Configuration.PlayerChatFormat
                                    .Replace("%message%", $"{msg.Content}")
								    .Replace("%playername%", $"[{this.Name}] {msg.Author.Username}")
								    .Replace("%worldname%", TCRCore.Game.World.GetName()));
                        }

                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write($"[{this.Name}] ");
						Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(msgout);
						Console.ResetColor();
						Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                PrettyPrint.Log("Error receiving data: " + ex.Message, ConsoleColor.Red);

                if (debug)
                    Console.WriteLine(ex);
            }

            return Task.CompletedTask;
        }

		private void Core_OnClientMessageReceived(object sender, ClientChatEventArgs e)
		{
            if (sender == this)
                return;
            
            var ChannelsToSendTo = Channel_IDs.Except(Endpoint.DenyReceivingMessagesFromGame);
			messageQueue.QueueMessage(ChannelsToSendTo,
				Configuration.PlayerChatFormat
								.Replace("%message%", $"{e.Message}")
								.Replace("%playername%", $"[{e.ClientName}] {e.User.Username}")
								.Replace("%worldname%", TCRCore.Game.World.GetName()));
		}

		/// <summary>
		/// A debug method that forcefully schedules the bot to retry connection.
		/// </summary>
		public void ForceFail()
		{
            ScheduleRetry(new Exception("This is a test"));
		}

        /// <summary>
        /// Updates the bot's now playing status periodically using a timer.
        /// </summary>
        private void GameStatusUpdate(object sender, ElapsedEventArgs e)
        {
            string status = Main.Config.GameStatus;
            status = status.Replace("%playercount%", TCRCore.Game.GetCurrentPlayerCount().ToString());
            status = status.Replace("%maxplayers%", TCRCore.Game.GetMaxPlayerCount().ToString());
            status = status.Replace("%worldname%", TCRCore.Game.World.GetName());

            Socket.SetGameAsync(status);
        }

        /// <summary>
        /// Sets a timer to retry after a specified time in the configuration.
        /// </summary>
        private Task ScheduleRetry(Exception e)
        {
            if(retryConnection == true || manualDisconnect == true)
                return Task.CompletedTask;

            retryConnection = true;
            PrettyPrint.Log($"Error: {e.Message}", ConsoleColor.Red);
            PrettyPrint.Log($"Attempting to reconnect in {Main.Config.SecondsToWaitBeforeRetryingAgain} seconds...", ConsoleColor.Yellow);
            var retryTimer = new System.Timers.Timer(Main.Config.SecondsToWaitBeforeRetryingAgain * 1000);
            retryTimer.Elapsed += (senderr, ee) =>
            {
                RetryAfterConnectionError();
                retryTimer.Stop();
                retryTimer.Dispose();
            };
            retryTimer.Start();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks the state of the socket. If it is in an error state, 
        /// it will dispose of the Discord ChatClient and reinitialize a new one from scratch.
        /// </summary>
        private void RetryAfterConnectionError()
        {
            if (Main.Config.NumberOfTimesToRetryConnectionAfterError < 0
                && fatalErrorCounter == 0)
            {
                PrettyPrint.Log($"Connection retry count set to infinite...", ConsoleColor.Yellow);
            }

            fatalErrorCounter++;

            try
            {
                if (Socket.ConnectionState == ConnectionState.Connected)
                    return;
            }
            catch
            {
                PrettyPrint.Log("Socket Error: Fatal exception", ConsoleColor.Red);
            }

            Disconnect();

            if (fatalErrorCounter >= Main.Config.NumberOfTimesToRetryConnectionAfterError
                && Main.Config.NumberOfTimesToRetryConnectionAfterError > 0)
            {
                PrettyPrint.Log($"Unable to establish a connection after {Main.Config.NumberOfTimesToRetryConnectionAfterError} attempts.", ConsoleColor.Red);
                PrettyPrint.Log("Please use the reload command to re-establish connection.", ConsoleColor.Red);
                return;
            }

            PrettyPrint.Log($"#{fatalErrorCounter} - Restarting client...", ConsoleColor.Yellow);
            var restartClient = new ChatClient(parent, Endpoint);
            restartClient.Reconnect = true;
            restartClient.ConnectAsync();
            parent.Add(restartClient);
            Dispose();
        }

        public override void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg)
        {
            if (errorCounter > 2)
                return;

            var ChannelsToSendTo = Channel_IDs.Except(Endpoint.DenyReceivingMessagesFromGame);
            if (ChannelsToSendTo.Count() <= 0)
                return;

            try
            {

                string outMsg = "";
                string bossName = "";

                if (msg.Player.PlayerId == -1 && msg.Message.EndsWith(" has joined."))
                    outMsg = Configuration.PlayerLoggedInFormat;
                else if (msg.Player.PlayerId == -1 && msg.Message.EndsWith(" has left."))
                    outMsg = Configuration.PlayerLoggedOutFormat;
                else if (msg.Player.Name != "Server" && msg.Player.PlayerId != -1)
                    outMsg = Configuration.PlayerChatFormat;
                else if (msg.Player.Name == "Server")
				{
                    if (msg.Player.PlayerId != -1)
                        outMsg = Configuration.PlayerChatFormat;
                    else if (msg.Message.EndsWith(" has awoken!"))
                        outMsg = Configuration.VanillaBossSpawned;
                    else if (msg.Message == "The server is starting!")
                        outMsg = Configuration.ServerStartingFormat;
                    else if (msg.Message == "The server is stopping!")
                        outMsg = Configuration.ServerStoppingFormat;
                    else if (msg.Message.Contains("A new version of TCR is available!"))
                        outMsg = ":desktop:  **%message%**";
                    else
                        outMsg = Configuration.WorldEventFormat;
                }
                else
                    outMsg = "%message%";

                if (msg.Player != null)
                    outMsg = outMsg.Replace("%playername%", msg.Player.Name)
                                   .Replace("%groupprefix%", msg.Player.GroupPrefix)
                                   .Replace("%groupsuffix%", msg.Player.GroupSuffix);

                outMsg = chatParser.RemoveTerrariaColorAndItemCodes(outMsg);

                if (msg.Message.EndsWith(" has awoken!"))
                {
                    bossName = msg.Message.Replace(" has awoken!", "");
                    outMsg = outMsg.Replace("%bossname%", bossName);
                }

                // Find the Player Name
                if (msg.Player == null && (msg.Message.EndsWith(" has joined.") || msg.Message.EndsWith(" has left.")))
                {
                    string playerName = msg.Message.Replace(" has joined.", "").Replace(" has left.", "");

                    // Suppress empty player name "has left" messages caused by port sniffers
                    if (playerName == null || playerName == "")
                    {
                        // An early return is the easiest way out
                        return;
                    }

                    outMsg = outMsg.Replace("%playername%", playerName);
                }

                outMsg = outMsg.Replace("%playercount%", TCRCore.Game.GetCurrentPlayerCount().ToString());
                outMsg = outMsg.Replace("%maxplayers%", TCRCore.Game.GetMaxPlayerCount().ToString());
                outMsg = outMsg.Replace("%worldname%", TCRCore.Game.World.GetName());
                outMsg = outMsg.Replace("%message%", msg.Message);

                //if (Main.Config.RegexMessageEnabled)
                //{
                //    foreach (KeyValuePair<string, string> regex in Main.Config.RegexMessageReplace)
                //    {
                //        outMsg = Regex.Replace(outMsg, regex.Key, regex.Value);
                //    }
                //}    
                
                if (outMsg == "" || outMsg == null)
                    return;

                messageQueue.QueueMessage(ChannelsToSendTo, outMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errorCounter++;

                if (errorCounter > 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Discord Client has been terminated. Please reload the mod to issue a reconnect.");
                    Console.ResetColor();
                }
            }
        }

        public override void HandleCommandOutput(ICommandPayload payload, string result, string sourceChannelId)
        {
            if (messageQueue == null)
			{
                Console.WriteLine("Error: Message queue is not available.");
                return;
            }

            result = result.Replace("</br>", "\n");
            result = result.Replace("</b>", "**");
            result = result.Replace("</i>", "*");
            result = result.Replace("</code>", "`");
            result = result.Replace("</box>", "```");
            result = result.Replace("</quote>", "> ");

            messageQueue.QueueMessage(ulong.Parse(sourceChannelId), result);
        }

        public override void SendMessageToClient(string msg, string sourceChannelId)
            => SendMessageToClient(msg, null, sourceChannelId);

        public async void SendMessageToClient(string msg, Embed embed, string sourceChannelId)
        {
            var channel = Socket.GetChannel(ulong.Parse(sourceChannelId));
            if (channel is SocketTextChannel)
            {
				try
				{
                    await ((SocketTextChannel)channel).SendMessageAsync(msg);
                }
                catch(Exception e)
				{
                    PrettyPrint.Log(e.Message);
				}
            }
        }
    }
}
