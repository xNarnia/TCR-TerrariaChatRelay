using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;
using TerrariaChatRelay.Helpers;
using Newtonsoft.Json;
using TerrariaChatRelay.Clients.Discord;
using Newtonsoft.Json.Linq;
using System.Net;
using WebSocketSharp;
using DiscordChatRelay.Helpers;
using System.Text.RegularExpressions;
using TerrariaChatRelay.Clients;
using TerrariaChatRelay;
using DiscordChatRelay.Models;

namespace DiscordChatRelay
{
    public class ChatClient : BaseClient
    {
        public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        public const string API_URL = "https://discordapp.com/api/v6";

        // Discord Variables
        public List<ulong> Channel_IDs { get; set; }
        private string BOT_TOKEN;
        private int? LastSequenceNumber = 0;
        private ChatParser chatParser { get; set; }
        private System.Timers.Timer heartbeatTimer { get; set; }

        // Message Queue
        private System.Timers.Timer messageQueueTimer { get; set; }
        private DiscordMessageQueue messageQueue { get; set; }
        private bool queueing { get; set; }

        // TCR Variables
        private List<IChatClient> parent { get; set; }
        private WebSocket Socket;
        private int errorCounter;

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
            messageQueue.OnReadyToSend += delegate (Dictionary<ulong, Queue<string>> messages) {
                foreach(var queue in messages)
                {
                    string output = "";

                    foreach(var msg in queue.Value)
                    {
                        output += msg + '\n';
                    }

                    if(output.Length > 2000)
                        output = output.Substring(0, 2000);

                    SendMessageToDiscordChannel(queue.Key, output);
                }
            };
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
                    Console.WriteLine(" Invalid Token: BOT_TOKEN");
                if (Channel_IDs.Contains(0))
                    Console.WriteLine(" Invalid Channel Id: 0");

                Console.WriteLine("  Config path: " + new Configuration().FileName);
                Console.ResetColor();
                Dispose();
                return;
            }

            errorCounter = 0;

            Socket = new WebSocket(GATEWAY_URL);
            Socket.Compression = CompressionMethod.Deflate;
            Socket.OnOpen += (object sender, EventArgs e) =>
            {
				PrettyPrint.Log("Discord", "Connection complete!");
                Socket.Send(DiscordMessageFactory.CreateLogin(BOT_TOKEN));
            };

            Socket.OnMessage += Socket_OnDataReceived;
            Socket.OnMessage += Socket_OnHeartbeatReceived;
            Socket.OnError += Socket_OnError;
			if(!debug)
				Socket.Log.Output = (_, __) => { };
			Socket.Connect();

            if(!Main.Config.FirstTimeMessageShown 
                || Main.Config.AlwaysShowFirstTimeMessage)
            {
                messageQueue.QueueMessage(Channel_IDs,
                    $"**This bot is powered by TerrariaChatRelay**\nUse {Main.Config.CommandPrefix}info for more commands!");
				Main.Config.FirstTimeMessageShown = true;
				Main.Config.SaveJson();
            }
		}

        /// <summary>
        /// Unsubscribes all WebSocket events, then releases all resources used by the WebSocket.
        /// </summary>
        public override void Disconnect()
        {
            Socket.OnMessage -= Socket_OnDataReceived;
            Socket.OnMessage -= Socket_OnHeartbeatReceived;
            Socket.OnError -= Socket_OnError;

            if (Socket.ReadyState != WebSocketState.Closed)
                Socket.Close();

            heartbeatTimer.Stop();
            heartbeatTimer.Dispose();
            heartbeatTimer = null;
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
                if(heartbeatTimer != null)
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
            var json = e.Data;

            if (json == null) return;
            if (json.Length <= 1) return;

            if (debug)
                Console.WriteLine("\n" + json + "\n");

            if(!DiscordMessageFactory.TryParseDispatchMessage(json, out var msg)) return;
            LastSequenceNumber = msg.SequenceNumber;

            var chatmsg = msg.GetChatMessageData();
            if(chatmsg != null && Channel_IDs.Contains(chatmsg.ChannelId))
            {
                if (!chatmsg.Author.IsBot)
                {
                    string msgout = chatmsg.Message;

                    // Lazy add commands until I take time to design a command service properly
                    //if (ExecuteCommand(chatmsg))
                    //    return;

                    msgout = chatParser.ConvertUserIdsToNames(msgout, chatmsg.UsersMentioned);
                    msgout = chatParser.ShortenEmojisToName(msgout);
                    msgout = $"<{chatmsg.Author.Username}> {msgout}";

                    NetHelpers.BroadcastChatMessageWithoutTCRFormattable(
                        ("[c/7489d8:Discord] - " + msgout), -1);

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

        /// <summary>
        /// Attempts to reconnect after receiving an error.
        /// </summary>
        private void Socket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Disconnect();

			var restartClient = new ChatClient(parent, BOT_TOKEN, Channel_IDs.ToArray());
			PrettyPrint.Log("Discord", "Client disconnected. Attempting to reconnect...");
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

				if (msg.Player == null && msg.Message.EndsWith(" has joined."))
					outMsg = Configuration.PlayerLoggedInFormat;
				else if (msg.Player == null && msg.Message.EndsWith(" has left."))
					outMsg = Configuration.PlayerLoggedOutFormat;
				else if (msg.Player != null)
					outMsg = Configuration.PlayerChatFormat;
				else if (msg.Player == null && msg.Message.EndsWith(" has awoken!"))
					outMsg = Configuration.VanillaBossSpawned;
				else if (msg.Player == null && msg.Message == "The server is starting!")
					outMsg = Configuration.ServerStartingFormat;
				else if (msg.Player == null && msg.Message == "The server is stopping!")
					outMsg = Configuration.ServerStoppingFormat;
				else if (msg.Player == null)
					outMsg = Configuration.WorldEventFormat;
				else if (msg.Player == null && msg.Message.Contains("A new version of TCR is available!"))
					outMsg = ":desktop:  **%message%**";
				else
					outMsg = "%message%";

				if (msg.Player != null)
					outMsg = outMsg.Replace("%playername%", msg.Player.name);

				if(msg.Message.EndsWith(" has awoken!"))
				{
					bossName = msg.Message.Replace(" has awoken!", "");
					outMsg = outMsg.Replace("%bossname%", bossName);
				}

                // Find the Player Name
				if(msg.Player == null && (msg.Message.EndsWith(" has joined.") || msg.Message.EndsWith(" has left.")))
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

				outMsg = outMsg.Replace("%worldname%", Terraria.Main.worldName);
				outMsg = outMsg.Replace("%message%", msg.Message);

				if (outMsg == "" || outMsg == null)
					return;

				messageQueue.QueueMessage(Channel_IDs, outMsg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errorCounter++;

                if(errorCounter > 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Discord Client has been terminated. Please reload the mod to issue a reconnect.");
                    Console.ResetColor();
                }
            }
        }

        public async void SendMessageToDiscordChannel(ulong channelId, string message)
        {
            message = message.Replace("\\", "\\\\");
            message = message.Replace("\"", "\\\"");
            message = message.Replace("\n", "\\n");
            string json = DiscordMessageFactory.CreateTextMessage(message);

            string response = await SimpleRequest.SendJsonDataAsync($"{API_URL}/channels/{channelId}/messages",
                new WebHeaderCollection()
                    {
                        { "Authorization", $"Bot {BOT_TOKEN}" }
                    }, json);

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

   //     public bool ExecuteCommand(MessageData chatmsg)
   //     {
   //         var message = chatmsg.Message;
   //         var prefix = TCR.Config.Discord.CommandPrefix;

   //         if (message.Length > 0)
   //         {
   //             if (message.StartsWith(prefix))
   //             {
   //                 message = message.Substring(prefix.Length, message.Length - 1);
   //             }
   //         }

			//if (message.StartsWith("cmd "))
			//{
			//	message = message.Replace("cmd ", "");
			//	//Main.ExecuteCommand(message, new TCRCommandCaller());
			//}

			//switch (message)
   //         {
   //             case "info":
   //                 messageQueue.QueueMessage(chatmsg.ChannelId,
   //                     $"**Command List**\n```\n{prefix}playing - See who's online\n{prefix}world - See world information```");
   //                 return true;
   //             case "playing":
   //                 var playersOnline = string.Join(", ", Main.player.Where(x => x.name.Length != 0).Select(x => x.name));

   //                 if (playersOnline == "")
   //                     playersOnline = "No players online!";

   //                 messageQueue.QueueMessage(chatmsg.ChannelId,
   //                     $"**Currently Playing:**\n```{playersOnline} ```");
   //                 return true;
   //             case "world":
   //                 messageQueue.QueueMessage(chatmsg.ChannelId,
   //                     $"**World:** {Main.worldName}\n```\nDifficulty: {(Main.expertMode == false ? "Normal" : "Expert")}\nHardmode: {(Main.hardMode == false ? "No" : "Yes")}\nEvil Type: {(WorldGen.crimson == false ? "Corruption" : "Crimson")}```");
   //                 return true;
   //             default:
			//		return false;
   //         }
   //     }

		//private class TCRCommandCaller : CommandCaller
		//{
		//	public CommandType CommandType => CommandType.Console;

		//	public Player Player => null;

		//	public void Reply(string text, Color color = default(Color))
		//	{
		//		string[] array = text.Split('\n');
		//		foreach (string value in array)
		//		{
		//			EventManager.RaiseTerrariaMessageReceived(null, -1, Color.Aqua, value);
		//		}
		//	}
		//}
	}
}
