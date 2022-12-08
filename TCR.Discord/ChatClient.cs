using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients;
using TerrariaChatRelay.Helpers;
using Newtonsoft.Json;
using TerrariaChatRelay.Clients.Discord;
using Newtonsoft.Json.Linq;
using System.Net;
using TCRDiscord.Helpers;
using System.Text.RegularExpressions;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;
using Discord.WebSocket;
using Discord;

namespace TCRDiscord
{
    public class ChatClient : BaseClient
    {
        public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        public const string API_URL = "https://discordapp.com/api/v6";

        // Discord Variables
        public List<ulong> Channel_IDs { get; set; }
        public bool Reconnect { get; set; } = false;
        private string BOT_TOKEN;
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

        // Other
        private bool debug = false;

        public ChatClient(List<IChatClient> _parent, string bot_token, ulong[] channel_ids)
            : base(_parent)
        {
            parent = _parent;
            BOT_TOKEN = bot_token;
            chatParser = new ChatParser();
            Channel_IDs = channel_ids.ToList();

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

                SendMessageToClient(output, queue.Key);
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
                PrettyPrint.Log("Discord", "Please update your Mod Config. Mod reload required.");

                if (BOT_TOKEN == "BOT_TOKEN")
                    PrettyPrint.Log("Discord", " Invalid Token: BOT_TOKEN", ConsoleColor.Yellow);
                if (Channel_IDs.Contains(0))
                    PrettyPrint.Log("Discord", " Invalid Channel Id: 0", ConsoleColor.Yellow);

                PrettyPrint.Log("Discord", "Config path: " + new Configuration().FileName);
                Console.ResetColor();
                Dispose();
                return;
            }

            if (Main.Config.OwnerUserId == 0 && Reconnect == false)
                PrettyPrint.Log("Discord", " Invalid Owner Id: 0", ConsoleColor.Yellow);

            errorCounter = 0;

            Socket = new DiscordSocketClient(new DiscordSocketConfig()
			{
                GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged,
                MessageCacheSize = 30,
                LogLevel = LogSeverity.Verbose
			});
            await Socket.LoginAsync(TokenType.Bot, BOT_TOKEN);
            await Socket.StartAsync();
            Socket.MessageReceived += ClientMessageReceived;
			Socket.Connected += ConnectionSuccessful;
            Socket.Disconnected += ScheduleRetry;

            await Task.Delay(-1);

            if (Main.Config.ShowPoweredByMessageOnStartup && !Reconnect)
            {
                messageQueue.QueueMessage(Channel_IDs,
                    $"**This bot is powered by TerrariaChatRelay**\nUse **{Main.Config.CommandPrefix}help** for more commands!");
                Main.Config.ShowPoweredByMessageOnStartup = true;
                Main.Config.SaveJson();
            }
        }

		private Task ConnectionSuccessful()
		{
            PrettyPrint.Log("Discord", "Connection Established!");
            errorCounter = 0;
            fatalErrorCounter = 0;
            retryConnection = false;
            Socket.Connected -= ConnectionSuccessful;
            return Task.CompletedTask;
		}

		/// <summary>
		/// Unsubscribes all WebSocket events, then releases all resources used by the WebSocket.
		/// </summary>
		public override void Disconnect()
        {
            manualDisconnect = true;

            // Detach queue from event and dispose
            if (messageQueue != null)
			{
                messageQueue.OnReadyToSend -= OnMessageReadyToSend;
                messageQueue.Clear();
            }
            messageQueue = null;

            // Detach events
            if(Socket != null)
			{
                Socket.MessageReceived -= ClientMessageReceived;
                Socket.StopAsync().GetAwaiter();
            }

            Socket = null;
        }

        /// <summary>
        /// Parses data when Discord sends a message.
        /// </summary>
        private Task ClientMessageReceived(SocketMessage msg)
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

                        var user = new TCRClientUser("Discord", msg.Author.Username, userPermission);
                        TerrariaChatRelay.Core.RaiseClientMessageReceived(this, user, "[c/7489d8:Discord] - ", msgout, Main.Config.CommandPrefix, msg.Channel.Id);

                        msgout = $"<{msg.Author.Username}> {msgout}";

                        if (Channel_IDs.Count > 1 && !command)
                        {
                            messageQueue.QueueMessage(
                                Channel_IDs.Where(x => x != msg.Channel.Id),
                                $"**[Discord]** <{msg.Author.Username}> {msg.Content}");
                        }

                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("[Discord] ");
                        Console.ResetColor();
                        Console.Write(msgout);
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                PrettyPrint.Log("Discord", "Error receiving data: " + ex.Message, ConsoleColor.Red);

                if (debug)
                    Console.WriteLine(ex);
            }

            return Task.CompletedTask;
        }

        public void ForceFail()
		{
            ScheduleRetry(new Exception("This is a test"));
		}

        /// <summary>
        /// Sets a timer to retry after a specified time in the configuration.
        /// </summary>
        private Task ScheduleRetry(Exception e)
        {
            if(retryConnection == true || manualDisconnect == true)
                return Task.CompletedTask;

            retryConnection = true;
            PrettyPrint.Log("Discord", $"Error: {e.Message}", ConsoleColor.Red);
            PrettyPrint.Log("Discord", $"Attempting to reconnect in {Main.Config.SecondsToWaitBeforeRetryingAgain} seconds...", ConsoleColor.Yellow);
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
                PrettyPrint.Log("Discord", $"Connection retry count set to infinite...", ConsoleColor.Yellow);
            }

            fatalErrorCounter++;

            try
            {
                if (Socket.ConnectionState == ConnectionState.Connected)
                    return;
            }
            catch
            {
                PrettyPrint.Log("Discord", "Socket Error: Fatal exception", ConsoleColor.Red);
            }

            Disconnect();

            if (fatalErrorCounter >= Main.Config.NumberOfTimesToRetryConnectionAfterError
                && Main.Config.NumberOfTimesToRetryConnectionAfterError > 0)
            {
                PrettyPrint.Log("Discord", $"Unable to establish a connection after {Main.Config.NumberOfTimesToRetryConnectionAfterError} attempts.", ConsoleColor.Red);
                PrettyPrint.Log("Discord", "Please use the reload command to re-establish connection.", ConsoleColor.Red);
                return;
            }

            PrettyPrint.Log("Discord", $"#{fatalErrorCounter} - Restarting client...", ConsoleColor.Yellow);
            var restartClient = new ChatClient(parent, BOT_TOKEN, Channel_IDs.ToArray());
            restartClient.Reconnect = true;
            restartClient.ConnectAsync();
            parent.Add(restartClient);
            Dispose();
        }

        public override void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg)
        {
            if (errorCounter > 2)
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

                outMsg = outMsg.Replace("%worldname%", TerrariaChatRelay.Game.World.GetName());
                outMsg = outMsg.Replace("%message%", msg.Message);

                if (outMsg == "" || outMsg == null)
                    return;

                messageQueue.QueueMessage(Channel_IDs, outMsg);
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

        public override void HandleCommand(ICommandPayload payload, string result, ulong sourceChannelId)
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

            messageQueue.QueueMessage(sourceChannelId, result);
        }

        public override void SendMessageToClient(string msg, ulong sourceChannelId)
            => SendMessageToClient(msg, null, sourceChannelId);

        public async void SendMessageToClient(string msg, Embed embed, ulong sourceChannelId)
        {
            var channel = Socket.GetChannel(sourceChannelId);
            if (channel is SocketTextChannel)
            {
				try
				{
                    await ((SocketTextChannel)channel).SendMessageAsync(msg);
                }
                catch(Exception e)
				{
                    PrettyPrint.Log("Discord", e.Message);
				}
            }
        }

        public override void GameMessageSentHandler(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
