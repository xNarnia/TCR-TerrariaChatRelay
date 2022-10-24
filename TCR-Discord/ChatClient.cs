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
using WebSocketSharp;
using TCRDiscord.Helpers;
using System.Text.RegularExpressions;
using TerrariaChatRelay;
using TCRDiscord.Models;
using TerrariaChatRelay.Command;
using WebSocketSharp.Server;

namespace TCRDiscord
{
    public class ChatClient : BaseClient
    {
        public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        public const string API_URL = "https://discordapp.com/api/v6";

        // Discord Variables
        public List<ulong> Channel_IDs { get; set; }
        public bool Reconnect { get; set; } = true;
        private string BOT_TOKEN;
        private int? LastSequenceNumber = 0;
        private ChatParser chatParser { get; set; }
        private System.Timers.Timer heartbeatTimer { get; set; }

        // Message Queue
        private DiscordMessageQueue messageQueue { get; set; }

        // TCR Variables
        private List<IChatClient> parent { get; set; }
        public WebSocket Socket;
        private int errorCounter;
        private static int fatalErrorCounter;

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
        public override void Connect()
        {
            if (BOT_TOKEN == "BOT_TOKEN" || Channel_IDs.Contains(0))
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

            if (Main.Config.OwnerUserId == 0)
                PrettyPrint.Log("Discord", " Invalid Owner Id: 0", ConsoleColor.Yellow);

            errorCounter = 0;

            Socket = new WebSocket(GATEWAY_URL);
            Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            Socket.Compression = CompressionMethod.Deflate;
            Socket.OnOpen += (object sender, EventArgs e) =>
            {
                PrettyPrint.Log("Discord", "Connection established. Logging in!");
                fatalErrorCounter = 0;
                Socket.Send(DiscordMessageFactory.CreateLogin(BOT_TOKEN));
            };

            Socket.OnMessage += Socket_OnDataReceived;
            Socket.OnMessage += Socket_OnHeartbeatReceived;
            Socket.OnError += Socket_OnError;
			Socket.OnClose += Socket_OnClose;
            if (!debug)
                Socket.Log.Output = (_, __) => { };
            else
                Socket.Log.Output = (logData, output) =>
                {
                    //PrettyPrint.Log("Discord", output);
                    //PrettyPrint.Log("Discord", logData.Message);
                };

            Socket.Connect();

            if (Main.Config.ShowPoweredByMessageOnStartup && !Reconnect)
            {
                messageQueue.QueueMessage(Channel_IDs,
                    $"**This bot is powered by TerrariaChatRelay**\nUse **{Main.Config.CommandPrefix}help** for more commands!");
                Main.Config.ShowPoweredByMessageOnStartup = true;
                Main.Config.SaveJson();
            }
        }

		/// <summary>
		/// Unsubscribes all WebSocket events, then releases all resources used by the WebSocket.
		/// </summary>
		public override void Disconnect()
        {
            // Detach queue from event and dispose
            if(messageQueue != null)
			{
                messageQueue.OnReadyToSend -= OnMessageReadyToSend;
                messageQueue.Clear();
            }
            messageQueue = null;

            // Dispose heartbeat timer
            if(heartbeatTimer != null)
			{
                heartbeatTimer.Stop();
                heartbeatTimer.Dispose();
            }
            heartbeatTimer = null;

            // Detach events
            if(Socket != null)
			{
                Socket.OnMessage -= Socket_OnDataReceived;
                Socket.OnMessage -= Socket_OnHeartbeatReceived;
                Socket.OnError -= Socket_OnError;
                Socket.OnClose -= Socket_OnClose;

                // Dispose WebSocket client
                if (Socket.ReadyState != WebSocketState.Closed)
                    Socket.Close();
            }

            Socket = null;
        }

        /// <summary>
        /// Handles the heartbeat acknowledgement when the server asks for it.
        /// </summary>
        private void Socket_OnHeartbeatReceived(object sender, MessageEventArgs e)
        {
            var json = e.Data;

            if (json.Length <= 1)
                return;

            if (!DiscordMessageFactory.TryParseMessage(json, out var msg))
                return;

            if (msg.OpCode == GatewayOpcode.Hello)
            {
                if (heartbeatTimer != null)
                    heartbeatTimer.Dispose();

                heartbeatTimer = new System.Timers.Timer(((JObject)msg.Data).Value<int>("heartbeat_interval") / 2);
                heartbeatTimer.Elapsed += (senderr, ee) =>
                {
                    Socket.Send(DiscordMessageFactory.CreateHeartbeat(GetLastSequenceNumber()));
                    if (errorCounter > 0)
                        errorCounter--;
                };
                heartbeatTimer.Start();

                Socket.Send(DiscordMessageFactory.CreateHeartbeat(GetLastSequenceNumber()));
            }
        }

        /// <summary>
        /// Parses data when Discord sends a message.
        /// </summary>
        private void Socket_OnDataReceived(object sender, MessageEventArgs e)
        {
            try
            {
                var json = e.Data;

                if (json == null) return;
                if (json.Length <= 1) return;

                if (debug)
                    Console.WriteLine("\n" + json + "\n");

                if (!DiscordMessageFactory.TryParseDispatchMessage(json, out var msg)) return;
                LastSequenceNumber = msg.SequenceNumber;

                var chatmsg = msg.GetChatMessageData();
                if (chatmsg != null && chatmsg.Message != "" && Channel_IDs.Contains(chatmsg.ChannelId))
                {
                    if (!chatmsg.Author.IsBot)
                    {
                        string msgout = chatmsg.Message;

                        // Lazy add commands until I take time to design a command service properly
                        //if (ExecuteCommand(chatmsg))
                        //    return;

                        if (!Core.CommandServ.IsCommand(msgout, Main.Config.CommandPrefix))
                        {
                            msgout = chatParser.ConvertUserIdsToNames(msgout, chatmsg.UsersMentioned);
                            msgout = chatParser.ShortenEmojisToName(msgout);
                        }

                        Permission userPermission;
                        if (chatmsg.Author.Id == Main.Config.OwnerUserId)
                            userPermission = Permission.Owner;
                        else if (Main.Config.AdminUserIds.Contains(chatmsg.Author.Id))
                            userPermission = Permission.Admin;
                        else if (Main.Config.ManagerUserIds.Contains(chatmsg.Author.Id))
                            userPermission = Permission.Manager;
                        else
                            userPermission = Permission.User;

                        var user = new TCRClientUser("Discord", chatmsg.Author.Username, userPermission);
                        TerrariaChatRelay.Core.RaiseClientMessageReceived(this, user, "[c/7489d8:Discord] - ", msgout, Main.Config.CommandPrefix, chatmsg.ChannelId);

                        msgout = $"<{chatmsg.Author.Username}> {msgout}";

                        if (Channel_IDs.Count > 1)
                        {
                            messageQueue.QueueMessage(
                                Channel_IDs.Where(x => x != chatmsg.ChannelId),
                                $"**[Discord]** <{chatmsg.Author.Username}> {chatmsg.Message}");
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
        }

        public void ForceFail()
		{
            Socket_OnError(this, null);
		}

        /// <summary>
        /// Logs connection close reason and attempts to reconnect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            PrettyPrint.Log("Discord", $"Connection Closed: Code {e.Code} - {e.Reason}", ConsoleColor.Red);
            ScheduleRetry();
        }

        /// <summary>
        /// Logs error and attempts to reconnect.
        /// </summary>
        private void Socket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            PrettyPrint.Log("Discord", "Socket Error: " + e.Message, ConsoleColor.Red);
            if (debug)
                Console.WriteLine(e.Exception);
            ScheduleRetry();
        }

        /// <summary>
        /// Sets a timer to retry after a specified time in the configuration.
        /// </summary>
        private void ScheduleRetry()
		{
            PrettyPrint.Log("Discord", $"Attempting to reconnect in {Main.Config.SecondsToWaitBeforeRetryingAgain} seconds...", ConsoleColor.Yellow);
            var retryTimer = new System.Timers.Timer(Main.Config.SecondsToWaitBeforeRetryingAgain * 1000);
            retryTimer.Elapsed += (senderr, ee) =>
            {
                RetryAfterConnectionError();
                retryTimer.Stop();
                retryTimer.Dispose();
            };
            retryTimer.Start();
        }

        /// <summary>
        /// Checks the state of the socket. If it is in an error state, 
        /// it will dispose of the Discord ChatClient and reinitialize a new one from scratch.
        /// </summary>
        private void RetryAfterConnectionError()
        {
            if(Main.Config.NumberOfTimesToRetryConnectionAfterError < 0
                && fatalErrorCounter == 0)
			{
                PrettyPrint.Log("Discord", $"Connection retry count set to infinite...", ConsoleColor.Yellow);
            }

            fatalErrorCounter++;

            try
			{
                if (Socket.IsAlive
                    && Socket.ReadyState != WebSocketState.Closed
                    && Socket.ReadyState != WebSocketState.Closing)
                    return;
            }
			catch
			{
                PrettyPrint.Log("Discord", "Socket Error: Fatal exception", ConsoleColor.Red);
            } 

            Disconnect();

            if(fatalErrorCounter >= Main.Config.NumberOfTimesToRetryConnectionAfterError 
                && Main.Config.NumberOfTimesToRetryConnectionAfterError > 0)
			{
                PrettyPrint.Log("Discord", $"Unable to establish a connection after {Main.Config.NumberOfTimesToRetryConnectionAfterError} attempts.", ConsoleColor.Red);
                PrettyPrint.Log("Discord", "Please use the reload command to re-establish connection.", ConsoleColor.Red);
                return;
            }

            PrettyPrint.Log("Discord", $"#{fatalErrorCounter} - Restarting client...", ConsoleColor.Yellow);
            var restartClient = new ChatClient(parent, BOT_TOKEN, Channel_IDs.ToArray());
            restartClient.Reconnect = true;
            restartClient.Connect();
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
                else if (msg.Player.Name == "Server" && msg.Message.EndsWith(" has awoken!"))
                    outMsg = Configuration.VanillaBossSpawned;
                else if (msg.Player.Name == "Server" && msg.Message == "The server is starting!")
                    outMsg = Configuration.ServerStartingFormat;
                else if (msg.Player.Name == "Server" && msg.Message == "The server is stopping!")
                    outMsg = Configuration.ServerStoppingFormat;
                else if (msg.Player.Name == "Server")
                    outMsg = Configuration.WorldEventFormat;
                else if (msg.Player.Name == "Server" && msg.Message.Contains("A new version of TCR is available!"))
                    outMsg = ":desktop:  **%message%**";
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
                    if (playerName.IsNullOrEmpty())
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

        public void SendMessageToClient(string msg, Embed embed, ulong sourceChannelId)
        {
            msg = msg.Replace("\\", "\\\\");
            msg = msg.Replace("\"", "\\\"");
            msg = msg.Replace("\n", "\\n");
            string json = DiscordMessageFactory.CreateTextMessage(msg, embed);

            SendJsonPayloadToDiscordChannel(json, sourceChannelId);
        }

        public async void SendJsonPayloadToDiscordChannel(string json, ulong channelId)
        {
            string response = null;
            try
            {
                if (debug)
                {
                    Console.WriteLine($"Sending JSON to: {API_URL}/channels/{channelId}/messages");
                    Console.WriteLine(json);
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                response = await SimpleRequest.SendJsonDataAsync($"{API_URL}/channels/{channelId}/messages",
                    new WebHeaderCollection()
                        {
                            { "Authorization", $"Bot {BOT_TOKEN}" }
                        }, json);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("(401) Unauthorized"))
                {
                    PrettyPrint.Log("Discord", "Unauthorized access to Discord server. Is your BOT_TOKEN correct?", ConsoleColor.Red);
                }
                else if (e.Message.Contains("(403) Forbidden"))
                {
                    PrettyPrint.Log("Discord", "Forbidden access to Discord channel. Are your Channel IDs & BOT permissions correct?", ConsoleColor.Red);
                }
                else
                {
                    PrettyPrint.Log("Discord", e.Message, ConsoleColor.Red);
                }
				if (debug)
				{
                    Console.WriteLine(e);
                }
            }

            if (debug)
            {
                Console.WriteLine(response);
            }
        }

        public override void GameMessageSentHandler(object sender, TerrariaChatEventArgs msg)
        {

        }

        public int? GetLastSequenceNumber()
        {
            return LastSequenceNumber;
        }
    }
}
