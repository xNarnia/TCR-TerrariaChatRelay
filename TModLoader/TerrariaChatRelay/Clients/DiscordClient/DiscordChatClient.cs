using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Discord;
using TerrariaChatRelay.Clients;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Helpers;
using System.Timers;
using Discord.Net;
using System.Configuration;
using TerrariaChatRelay.Clients.DiscordClient.Helpers;
using TerrariaChatRelay.Clients.DiscordClient.Messaging;

namespace TerrariaChatRelay.Clients.DiscordClient
{
	public class DiscordChatClient : BaseClient
	{
		public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
		public const string API_URL = "https://discordapp.com/api/v6";

		// Discord Variables
		public DiscordSocketClient Socket { get; set; }
		public Endpoint Endpoint { get; set; }
		public List<ulong> Channel_IDs { get; set; }
		public List<string> SendOnlyChannel_IDs { get; set; }
		public List<string> ReceiveOnlyChannel_IDs { get; set; }
		public bool Reconnect { get; set; } = false;
		private string BOT_TOKEN;
		private ChatParser chatParser { get; set; }
		private DiscordMessageQueue messageQueue { get; set; }
		public Timer channelTitleTimer { get; set; }

		// TCR Variables
		private List<IChatClient> parent { get; set; }
		public override string Name { get; set; } = "Discord";

		private int errorCounter;
		private static int fatalErrorCounter;
		private bool retryConnection = false;

		// Other
		private bool debug = false;

		public DiscordChatClient(List<IChatClient> _parent, Endpoint _endpoint)
			: base(_parent)
		{
			parent = _parent;
			BOT_TOKEN = _endpoint.BotToken;
			chatParser = new ChatParser();
			Channel_IDs = _endpoint.Channel_IDs.ToList();
			Endpoint = _endpoint;
			channelTitleTimer = new Timer(60000);

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

				var embed =
					new EmbedBuilder()
						.WithDescription(output)
						.Build();
				SendMessageToClient("", embed, queue.Key.ToString());
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

				PrettyPrint.Log("Discord", "Config path: " + new DiscordConfig().FileName);
				Console.ResetColor();
				Dispose();
				return;
			}

			if (DiscordPlugin.Config.OwnerUserId == 0 && Reconnect == false)
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
			await Socket.SetGameAsync(DiscordPlugin.Config.BotStatus);
			channelTitleTimer.Elapsed += ChannelTitleTimer_Elapsed;
			channelTitleTimer.Start();
		}

		private async void ChannelTitleTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			foreach (var channelId in Channel_IDs)
			{
				var channel = await Socket.GetChannelAsync(channelId);
				if (channel is SocketTextChannel)
				{
					var textChannel = (SocketTextChannel)channel;
					var players = Terraria.Main.player.Where(x => x.name.Length != 0);
					var topic = DiscordPlugin.Config.BotChannelDescription;
					topic =
						topic.Replace("%worldname%", GameHelper.World.GetName())
							.Replace("%playercount%", players.Count().ToString())
							.Replace("%maxplayers%", Terraria.Main.maxNetPlayers.ToString());

					if (topic == textChannel.Topic)
					{
						return;
					}

					try
					{
						await textChannel.ModifyAsync(x => x.Topic = topic);
					}
					catch (HttpException ex)
					{
						if (ex.Message.ToLower().Contains("missing permissions"))
						{
							PrettyPrint.Log("Discord", "Missing Permission - Manage Channels: Could not update channel description.", ConsoleColor.Red);
							PrettyPrint.Log("Discord", $"   Update permissions then restart using {DiscordPlugin.Config.CommandPrefix}restart.", ConsoleColor.Red);
							channelTitleTimer.Stop();
						}
						else
						{
							channelTitleTimer.Stop();
							throw;
						}
					}
				}
			}
		}

		private Task ConnectionSuccessful()
		{
			PrettyPrint.Log("Discord", "Connection Established!");

			if (DiscordPlugin.Config.ShowPoweredByMessageOnStartup && !Reconnect)
			{
				messageQueue.QueueMessage(Channel_IDs,
					$"**This bot is powered by TerrariaChatRelay**\nUse **{DiscordPlugin.Config.CommandPrefix}help** for more commands!");
			}

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
				Task.Run(async () =>
				{
					await Socket.StopAsync();
					await Socket.DisposeAsync();
				});
				Socket.MessageReceived -= ClientMessageReceived;
			}

			Socket = null;

			channelTitleTimer.Stop();
			channelTitleTimer.Dispose();
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

						// Lazy add commands until I take time to design a command service properly
						//if (ExecuteCommand(chatmsg))
						//    return;

						bool command = Core.CommandServ.IsCommand(msgout, DiscordPlugin.Config.CommandPrefix);

						if (!command)
						{
							if (Endpoint.DenySendingMessagesToGame.Contains(msg.Channel.Id))
								return Task.CompletedTask;

							msgout = chatParser.ConvertUserIdsToNames(msgout, msg.MentionedUsers);
							msgout = chatParser.ShortenEmojisToName(msgout);
						}

						Permission userPermission;
						if (msg.Author.Id == DiscordPlugin.Config.OwnerUserId
							|| DiscordPlugin.Config.OwnerUserId == 0 && msg.Author is SocketGuildUser && msg.Author.Id == ((SocketGuildUser)msg.Author).Guild.OwnerId) // If no owner id is specified, use the guild owner id
							userPermission = Permission.Owner;
						else if (DiscordPlugin.Config.AdminUserIds.Contains(msg.Author.Id))
							userPermission = Permission.Admin;
						else if (DiscordPlugin.Config.ManagerUserIds.Contains(msg.Author.Id))
							userPermission = Permission.Manager;
						else
							userPermission = Permission.User;

						var user = new TCRClientUser(Name, msg.Author.Username, userPermission);

						// There needs to be a better way of doing this rather than making exceptions that couple to commands
						if (command)
						{
							string[] channelCommands = { "denysend", "denyreceive", "allowsend", "allowreceive" };

							foreach (var commandKey in channelCommands)
							{
								if (msgout.StartsWith(DiscordPlugin.Config.CommandPrefix + commandKey))
								{
									msgout = DiscordPlugin.Config.CommandPrefix + commandKey + " " + msg.Channel.Id;
								}
							}
						}

						Core.RaiseClientMessageReceived(this, user, Name, DiscordConfig.TerrariaInGameDiscordPrefix, msgout, DiscordPlugin.Config.CommandPrefix, msg.Channel.Id.ToString());

						msgout = $"<{msg.Author.Username}> {msgout}";

						if (Channel_IDs.Count > 1)
						{
							messageQueue.QueueMessage(
								Channel_IDs.Where(x => x != msg.Channel.Id && !Endpoint.DenyReceivingMessagesFromGame.Contains(x)),
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
			//Socket_OnError(this, null);
		}

		/// <summary>
		/// Sets a timer to retry after a specified time in the configuration.
		/// </summary>
		private Task ScheduleRetry(Exception e)
		{
			if (retryConnection == true)
				return Task.CompletedTask;

			retryConnection = true;
			PrettyPrint.Log("Discord", $"Error: {e.Message}", ConsoleColor.Red);
			PrettyPrint.Log("Discord", $"Attempting to reconnect in {DiscordPlugin.Config.SecondsToWaitBeforeRetryingAgain} seconds...", ConsoleColor.Yellow);
			var retryTimer = new Timer(DiscordPlugin.Config.SecondsToWaitBeforeRetryingAgain * 1000);
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
			if (DiscordPlugin.Config.NumberOfTimesToRetryConnectionAfterError < 0
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

			if (fatalErrorCounter >= DiscordPlugin.Config.NumberOfTimesToRetryConnectionAfterError
				&& DiscordPlugin.Config.NumberOfTimesToRetryConnectionAfterError > 0)
			{
				PrettyPrint.Log("Discord", $"Unable to establish a connection after {DiscordPlugin.Config.NumberOfTimesToRetryConnectionAfterError} attempts.", ConsoleColor.Red);
				PrettyPrint.Log("Discord", "Please use the reload command to re-establish connection.", ConsoleColor.Red);
				return;
			}

			PrettyPrint.Log($"#{fatalErrorCounter} - Restarting client...", ConsoleColor.Yellow);
			var restartClient = new DiscordChatClient(parent, Endpoint);
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
					outMsg = DiscordConfig.PlayerLoggedInFormat;
				else if (msg.Player.PlayerId == -1 && msg.Message.EndsWith(" has left."))
					outMsg = DiscordConfig.PlayerLoggedOutFormat;
				else if (msg.Player.Name != "Server" && msg.Player.PlayerId != -1)
					outMsg = DiscordConfig.PlayerChatFormat;
				else if (msg.Player.Name == "Server")
				{
					if (msg.Player.PlayerId != -1)
						outMsg = DiscordConfig.PlayerChatFormat;
					else if (msg.Message.EndsWith(" has awoken!"))
						outMsg = DiscordConfig.VanillaBossSpawned;
					else if (msg.Message == "The server is starting!")
						outMsg = DiscordConfig.ServerStartingFormat;
					else if (msg.Message == "The server is stopping!")
						outMsg = DiscordConfig.ServerStoppingFormat;
					else if (msg.Message.Contains("A new version of TCR is available!"))
						outMsg = ":desktop:  **%message%**";
					else
						outMsg = DiscordConfig.WorldEventFormat;
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

				outMsg = outMsg.Replace("%worldname%", GameHelper.World.GetName());
				outMsg = outMsg.Replace("%message%", msg.Message);

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

		/// <summary>
		/// Event for when a command is ran.
		/// </summary>
		/// <param name="payload">Command that was ran.</param>
		/// <param name="result">Output from TCR command. Can be empty or null.</param>
		/// <param name="sourceChannelId">Channel the command was executed from.</param>
		public override void HandleCommandOutput(ICommandPayload payload, string result, string sourceChannelId)
		{
			if (result == "" || result == null)
				return;

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
			=> SendMessageToClient("", new EmbedBuilder().WithDescription(msg).Build(), sourceChannelId);

		public async void SendMessageToClient(string msg, Embed embed, string sourceChannelId)
		{
			var channel = Socket.GetChannel(ulong.Parse(sourceChannelId));
			if (channel is SocketTextChannel)
			{
				try
				{
					await ((SocketTextChannel)channel).SendMessageAsync(msg, false, embed);
				}
				catch (Exception e)
				{
					PrettyPrint.Log(e.Message);
				}
			}
		}
	}
}