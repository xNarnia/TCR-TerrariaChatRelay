using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCRCore.Clients;
using TCRCore.Helpers;
using TCRSlack.Helpers;
using TCRCore;
using TCRCore.Command;
using System.Timers;
using Slack.NetStandard.Auth;
using Slack.NetStandard.AsyncEnumerable;
using Newtonsoft.Json;
using Slack.NetStandard;
using Slack.NetStandard.EventsApi.CallbackEvents;
using Slack.NetStandard.Objects;
using System.Net.WebSockets;
using Slack.NetStandard.WebApi.Chat;
using Slack.NetStandard.EventsApi;
using Slack.NetStandard.Socket;
using Slack.NetStandard.WebApi.Conversations;
using System.Data.SqlTypes;

namespace TCRSlack
{
    public class ChatClient : BaseClient
	{
		public override string Name { get; set; } = "Slack";

		// Slack Variables
		public List<string> Channel_IDs { get; set; }
        public List<string> SendOnlyChannel_IDs { get; set; }
        public List<string> ReceiveOnlyChannel_IDs { get; set; }
        public Endpoint Endpoint { get; set; }
		public Dictionary<string, User> Users { get; set; }
		public Dictionary<string, Channel> Channels { get; set; }
		public bool Reconnect { get; set; } = false;
        private string xapp_BOT_TOKEN { get; set; }
		private string xoxb_BOT_TOKEN { get; set; }
		private ChatParser chatParser { get; set; }

		// Message Queue
		private MessageQueue messageQueue { get; set; }

        // TCR Variables
        private List<IChatClient> parent { get; set; }
        private SocketModeClient Socket;
		private SlackWebApiClient Api;
		private int errorCounter;
        private static int fatalErrorCounter;
        private bool retryConnection = false;
        private bool manualDisconnect = false;

		// Other
		private bool debug = false;

        public ChatClient(List<IChatClient> _parent, Endpoint _endpoint)
            : base(_parent)
        {
            parent = _parent;
            xapp_BOT_TOKEN = _endpoint.xapp_BotToken;
            xoxb_BOT_TOKEN = _endpoint.xoxb_BotToken;
			Channel_IDs = _endpoint.Channel_IDs.ToList();
            Endpoint = _endpoint;
            chatParser = new ChatParser();

            messageQueue = new MessageQueue(700);
            messageQueue.OnReadyToSend += OnMessageReadyToSend;
        }

        /// <summary>
        /// Event fired when a message from in-game is received.
        /// Queues messages to stack messages closely sent to each other.
        /// This will allow TCR to combine messages and reduce messages sent to Slack.
        /// </summary>
        public void OnMessageReadyToSend(Dictionary<string, Queue<string>> messages)
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
        /// Create a new WebSocket and initiate connection with Slack servers. 
        /// Utilizes BOT_TOKEN and CHANNEL_ID found in Mod Config.
        /// </summary>
        public override async void ConnectAsync()
        {
            if ((xapp_BOT_TOKEN == "BOT_TOKEN" || Channel_IDs.Contains("")) && Reconnect == false)
            {
                PrettyPrint.Log("Please update your Mod Config. Mod reload required.");

                if (xapp_BOT_TOKEN == "BOT_TOKEN")
                    PrettyPrint.Log(" Invalid Token: BOT_TOKEN", ConsoleColor.Yellow);
                if (Channel_IDs.Contains(""))
                    PrettyPrint.Log(" Invalid Channel Id: 0", ConsoleColor.Yellow);

                PrettyPrint.Log("Config path: " + new Configuration().FileName);
                Console.ResetColor();
                Dispose();
                return;
            }

			if ((Main.Config.OwnerUser == "" || Main.Config.OwnerUser == null) && Reconnect == false)
                PrettyPrint.Log(" Invalid Owner Id", ConsoleColor.Yellow);

            errorCounter = 0;

			try
            {
				Socket = new SocketModeClient();

				Api = new SlackWebApiClient(Endpoint.xoxb_BotToken);

				// Gets you your user list
				var users = await Api.Users.List();
				if (users.OK)
					Users = users.Members.ToDictionary(x => x.ID);
				else
				{
					PrettyPrint.Log($"[ERROR] Users could not be loaded: {users.Error}", ConsoleColor.Red);
                    return;
				}


				// Gets you your user list
				var channels = await Api.Conversations.List(new ConversationListRequest() { ExcludeArchived = true });
                if (channels.OK)
                    Channels = channels.Channels.ToDictionary(x => x.ID);
                else
                {
					PrettyPrint.Log($"[ERROR] Channels could not be loaded: {channels.Error}", ConsoleColor.Red);
                    return;
				}

				var Client = Socket.WebSocket;
				var cancel = new System.Threading.CancellationTokenSource();
				await Socket.ConnectAsync(Endpoint.xapp_BotToken, cancel.Token);
                await ConnectionSuccessful();
				Core.OnClientMessageReceived += Core_OnClientMessageReceived;

				ProcessMessages(Socket, cancel.Token).GetAwaiter();
			}
			catch (Exception ex)
            {
                PrettyPrint.Log(ex.Message);
            }
        }

		private Task ConnectionSuccessful()
		{
            PrettyPrint.Log("Connection Established!", ConsoleColor.Green);
            errorCounter = 0;
            fatalErrorCounter = 0;
            retryConnection = false;

			if (Main.Config.ShowPoweredByMessageOnStartup && !Reconnect)
			{
				messageQueue.QueueMessage(Channel_IDs,
					$"*This bot is powered by TerrariaChatRelay*\nUse *{Main.Config.CommandPrefix}help* for more commands!");
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

            // Detach queue from event and dispose
            if (messageQueue != null)
			{
                messageQueue.OnReadyToSend -= OnMessageReadyToSend;
                messageQueue.Clear();
            }
            messageQueue = null;

            // Detach events
            if (Socket != null)
			{
                Socket.Dispose();
            }

            if (Api != null)
            {
                Api.Client.Dispose();
                Api = null;
            }

            Socket = null;
        }

		private async Task ProcessMessages(SocketModeClient client, System.Threading.CancellationToken token)
		{
			await foreach (var envelope in client.EnvelopeAsyncEnumerable(token))
			{
                if (manualDisconnect)
                    return;

                if(Socket.WebSocket.State == WebSocketState.Closed)
                {
                    var closeStatus = Socket.WebSocket.CloseStatus;
					await ScheduleRetry(closeStatus.HasValue ? new Exception(closeStatus.Value.ToString()) : new Exception("Socket closed."));
                    return;
                }

				var ack = new Acknowledge { EnvelopeId = envelope.EnvelopeId }; //All messages must be acknowledged within a few seconds
				await Socket.Send(JsonConvert.SerializeObject(ack));

				if (envelope.Payload is EventCallback)
				{
					EventCallback cb = (EventCallback)envelope.Payload;

					if (cb.Event is MessageCallbackEvent && cb.Event.Type == "message")
					{
						var ev = (MessageCallbackEvent)cb.Event;
						await SlackMessageReceivedHandler(ev);
					}
				}
			}
		}

		/// <summary>
		/// Parses data when Slack sends a message.
		/// </summary>
		/// 
		private Task SlackMessageReceivedHandler(MessageCallbackEvent msg)
		{
			try
			{
				if (msg.Text == null
					|| msg.Text.Length <= 1)
					return Task.CompletedTask;

				if (debug)
					Console.WriteLine("\n" + msg.Text + "\n");

                if (!Channels.ContainsKey(msg.Channel) || !Users.ContainsKey(msg.User))
                    return Task.CompletedTask;

				var ChannelName = Channels[msg.Channel].Name;
				var User = Users[msg.User];

				if (msg != null && msg.Text != "" && Channel_IDs.Contains(ChannelName))
				{
					if (!User.IsBot.Value)
                    {
						string msgout = msg.Text;
						bool command = Core.CommandServ.IsCommand(msgout, Main.Config.CommandPrefix);

						if (!command)
						{
							if (Endpoint.DenySendingMessagesToGame.Contains(ChannelName))
								return Task.CompletedTask;

							//msgout = chatParser.ConvertUserIdsToNames(msgout, msg.MentionedUsers);
							msgout = chatParser.ShortenEmojisToName(msgout);
						}

						Permission userPermission;
						if (User.Name == Main.Config.OwnerUser)
							userPermission = Permission.Owner;
						else if (Main.Config.AdminUsers.Contains(User.Name))
							userPermission = Permission.Admin;
						else if (Main.Config.ManagerUsers.Contains(User.Name))
							userPermission = Permission.Manager;
						else
							userPermission = Permission.User;

						var user = new TCRClientUser(this.Name, User.RealName, userPermission);

						// There needs to be a better way of doing this rather than making exceptions that couple to commands
						if (command)
						{
							string[] channelCommands = { "denysend", "denyreceive", "allowsend", "allowreceive" };

							foreach (var commandKey in channelCommands)
							{
								if (msgout.StartsWith(Main.Config.CommandPrefix + commandKey))
								{
									msgout = Main.Config.CommandPrefix + commandKey + " " + ChannelName;
								}
							}
						}

						TCRCore.Core.RaiseClientMessageReceived(this, user, this.Name, Configuration.TerrariaInGameSlackPrefix, msgout, Main.Config.CommandPrefix, ChannelName);

						msgout = $"<{User.Name}> {msgout}";

						if (Channel_IDs.Count > 1 && !command)
						{
							messageQueue.QueueMessage(
								Channel_IDs.Where(x => x != ChannelName && !Endpoint.DenyReceivingMessagesFromGame.Contains(x)),
                                Configuration.PlayerChatFormat
                                .Replace("%message%", $"{msg.Text}")
                                .Replace("%playername%", $"[{this.Name}] {User.RealName}")
								.Replace("%worldname%", TCRCore.Game.World.GetName()));
						}

						Console.ForegroundColor = ConsoleColor.Yellow;
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
        /// it will dispose of the Slack ChatClient and reinitialize a new one from scratch.
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
                if (Socket.WebSocket.State != WebSocketState.Closed)
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
                        outMsg = ":desktop:  *%message%*";
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
                    Console.WriteLine("Slack Client has been terminated. Please reload the mod to issue a reconnect.");
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
            result = result.Replace("</b>", "*");
            result = result.Replace("</i>", "_");
            result = result.Replace("</code>", "`");
            result = result.Replace("</box>", "```");
            result = result.Replace("</quote>", "> ");

            messageQueue.QueueMessage(sourceChannelId, result);
        }

		public override void SendMessageToClient(string msg, string sourceChannelId = "")
		{
			try
			{
				var message = new PostMessageRequest
				{
					Text = msg,
					Channel = sourceChannelId
				};

                Api.Chat.Post(message);
            }
            catch(Exception e)
			{
                PrettyPrint.Log(e.Message);
			}
        }
	}
}
