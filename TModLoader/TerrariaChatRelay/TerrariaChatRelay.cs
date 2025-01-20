using System;
using System.IO;
using System.Linq;
using System.Net;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using System.Net.Http;
using Terraria.IO;
using Terraria.GameContent.NetModules;
using Newtonsoft.Json;
using TerrariaChatRelay.Helpers;
using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay
{
	public class TerrariaChatRelay : Mod
	{
		public Version LatestVersion = new Version("0.0.0.0");
		public string PlayerJoinEndingString;
		public string PlayerLeaveEndingString;
		private Mod SubworldLib;

		public TerrariaChatRelay()
		{
		}

		public override void Load()
		{
			base.Load();

			// Set security protocol to allow requests to GitHub for version checking
			ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			// Config handling
			var config = ModContent.GetInstance<Config>();
			string modConfigFilePath = Path.Combine(Main.SavePath, "ModConfigs", $"{Name}_Config.json");
			string discordConfigPath = Path.Combine(Main.SavePath, "ModConfigs", "TerrariaChatRelay", "TerrariaChatRelay-Discord.json");
			
			// Compare times to see which is newest. Use newest config.
			DateTime mConfigWriteTime = File.GetLastWriteTime(modConfigFilePath);
			DateTime dConfigWriteTime = File.GetLastWriteTime(discordConfigPath);

			if (mConfigWriteTime > dConfigWriteTime)
			{
				FileInfo file = new FileInfo(discordConfigPath);
				file.Directory.Create();
				File.WriteAllText(discordConfigPath, JsonConvert.SerializeObject(config));
				new DiscordConfig().GetOrCreateConfiguration().SaveJson(); // Used to propogate comments through file
			}

			Global.Config = new TCRConfig().GetOrCreateConfiguration();

			// Hooks
			On_ChatCommandProcessor.ProcessIncomingMessage += On_ChatCommandProcessor_ProcessIncomingMessage;
			On_ChatHelper.BroadcastChatMessage += BroadcastChatMessage;
			On_WorldFile.LoadWorld_Version2 += OnWorldLoadStart;
			On_Netplay.StopListening += OnServerStop;
			On_NetMessage.SyncConnectedPlayer += OnPlayerJoin_NetMessage_SyncConnectedPlayer;
			On_RemoteClient.Reset += RemoteClient_Reset;
			On_NetMessage.greetPlayer += NetMessage_greetPlayer;

			PlayerJoinEndingString = Language.GetText("LegacyMultiplayer.19").Value.Split(new string[] { "{0}" }, StringSplitOptions.None).Last();
			PlayerLeaveEndingString = Language.GetText("LegacyMultiplayer.20").Value.Split(new string[] { "{0}" }, StringSplitOptions.None).Last();

			// Add subscribers to list
			Core.Initialize(new Adapter());
			Core.ConnectClients();

			if (Global.Config.CheckForLatestVersion)
				Task.Run(GetLatestVersionNumber);
		}

		public override void PostSetupContent()
		{
			base.PostSetupContent();

			// Mod References
			ModLoader.TryGetMod("SubworldLibrary", out SubworldLib);
		}

		/// <summary>
		/// Event for catching in-game player chat messages.
		/// </summary>
		private void On_ChatCommandProcessor_ProcessIncomingMessage(On_ChatCommandProcessor.orig_ProcessIncomingMessage orig, ChatCommandProcessor self, ChatMessage message, int clientId)
		{
			// If SubworldLib is present, remove the hook from Subworlds
			// This prevents double posting, allowing the main server to relay for both worlds
			if (SubworldLib != null)
			{
				object current = SubworldLib.Call("Current");
				if (current?.ToString().ToLower() != "false")
				{
					On_ChatCommandProcessor.ProcessIncomingMessage -= On_ChatCommandProcessor_ProcessIncomingMessage;
					orig(self, message, clientId);
					return;
				}
			}

			// Not relaying commands with / as those are typically for commands with sensitive information
			if (Global.Config.ShowChatMessages && !message.Text.StartsWith("/"))
			{
				Core.RaiseTerrariaMessageReceived(this, new TCRPlayer()
				{
					PlayerId = clientId,
					Name = Main.player[clientId].name
				}, message.Text);
			}
			orig(self, message, clientId);
		}

		/// <summary>
		/// Event for catching players entering before they are loaded in.
		/// </summary>
		private void NetMessage_greetPlayer(On_NetMessage.orig_greetPlayer orig, int plr)
		{
			NetPacket packet = NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("This chat is powered by TerrariaChatRelay"), Color.LawnGreen, byte.MaxValue);
			NetManager.Instance.SendToClient(packet, plr);
			orig(plr);
		}

		/// <summary>
		/// Event for catching players leaving.
		/// </summary>
		private void RemoteClient_Reset(On_RemoteClient.orig_Reset orig, RemoteClient self)
		{
			if (self.Id >= 0)
			{
				if (Main.player[self.Id].name != "")
				{
					var tcrPlayer = Main.player[self.Id].ToTCRPlayer(-1);
					Core.RaiseTerrariaMessageReceived(this, tcrPlayer, $"{tcrPlayer.Name} has left.");
				}
			}
			orig(self);
		}

		/// <summary>
		/// Event for catching players entering after they have loaded in.
		/// </summary>
		private void OnPlayerJoin_NetMessage_SyncConnectedPlayer(On_NetMessage.orig_SyncConnectedPlayer orig, int plr)
		{
			orig(plr);
			var tcrPlayer = Main.player[plr].ToTCRPlayer(-1);
			Core.RaiseTerrariaMessageReceived(this, tcrPlayer, $"{tcrPlayer.Name} has joined.");
		}

		/// <summary>
		/// <para>Loads the build.txt from GitHub to check if there is a newer version of TCR available. </para>
		/// If there is, a message will be displayed on the console and prepare a message for sending when the world is loading.
		/// </summary>
		public async Task GetLatestVersionNumber()
		{
			var client = new HttpClient();
			LatestVersion = this.Version;

			HttpResponseMessage res;
			try
			{
				res = await client.GetAsync("https://raw.githubusercontent.com/xNarnia/TCR-TerrariaChatRelay/master/version.txt");
			}
			catch (Exception)
			{
				return;
			}

			using (StreamReader sr = new StreamReader(res.Content.ReadAsStream()))
			{
				string buildtxt = sr.ReadToEnd();
				buildtxt = buildtxt.ToLower();

				string line = "";
				using (StringReader stringreader = new StringReader(buildtxt))
				{
					do
					{
						line = stringreader.ReadLine();
						if (line.Contains("version"))
						{
							line = line.Replace(" ", "");
							line = line.Replace("version=", "");

							LatestVersion = new Version(line);
							if (LatestVersion > Version)
								PrettyPrint.Log("Adapter", $"A new version of TCR is available: V.{LatestVersion.ToString()}");

							line = null;
						}
					}
					while (line != null);
				}
			}
		}

		/// <summary>
		/// Handle disconnect for all clients, remove events, and finally dispose of config.
		/// </summary>
		public override void Unload()
		{
			Core.DisconnectClients();
			On_ChatCommandProcessor.ProcessIncomingMessage -= On_ChatCommandProcessor_ProcessIncomingMessage;
			On_ChatHelper.BroadcastChatMessage -= BroadcastChatMessage;
			On_WorldFile.LoadWorld_Version2 -= OnWorldLoadStart;
			On_Netplay.StopListening -= OnServerStop;
			On_NetMessage.SyncConnectedPlayer -= OnPlayerJoin_NetMessage_SyncConnectedPlayer;
			On_RemoteClient.Reset -= RemoteClient_Reset;
			On_NetMessage.greetPlayer -= NetMessage_greetPlayer;
			Global.Config = null;
		}

		/// <summary>
		/// Event to send a message when the server is loading.
		/// </summary>
		private int OnWorldLoadStart(On_WorldFile.orig_LoadWorld_Version2 orig, BinaryReader reader)
		{
			try
			{
				if (!Netplay.Disconnect)
				{
					if (Global.Config.ShowServerStartMessage)
						Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, "The server is starting!");

					if (LatestVersion > Version)
						Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, $"A new version of TCR is available: V.{LatestVersion.ToString()}");
				}
			}
			catch (Exception e)
			{
				PrettyPrint.Log("Adapter", "Error checking for version update: " + e.Message, ConsoleColor.Red);
			}

			return orig(reader);
		}

		/// <summary>
		/// Event for detecting server stopping.
		/// </summary>
		private void OnServerStop(On_Netplay.orig_StopListening orig)
		{
			if (Global.Config.ShowServerStopMessage)
				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, "The server is stopping!");

			orig();
		}

		/// <summary>
		/// Event to intercept all other messages from Terraria. E.g. blood moon, death notifications, and player join/leaves.
		/// </summary>
		private void BroadcastChatMessage(On_ChatHelper.orig_BroadcastChatMessage orig, NetworkText text, Color color, int excludedPlayer)
		{
			if (Global.Config.ShowGameEvents && !text.ToString().EndsWith(PlayerJoinEndingString) && !text.ToString().EndsWith(PlayerLeaveEndingString))
				Core.RaiseTerrariaMessageReceived(this, (excludedPlayer > 0 ? Main.player[excludedPlayer].ToTCRPlayer(excludedPlayer) : TCRPlayer.Server), text.ToString());

			orig(text, color, excludedPlayer);
		}

		/// <summary>
		/// Event to intercept chat messages sent from players.
		/// </summary>
		private bool NetTextModule_DeserializeAsServer(On_NetTextModule.orig_DeserializeAsServer orig, Terraria.GameContent.NetModules.NetTextModule self, BinaryReader reader, int senderPlayerId)
		{
			long savedPosition = reader.BaseStream.Position;
			ChatMessage message = ChatMessage.Deserialize(reader);

			if (Global.Config.ShowChatMessages)
				Core.RaiseTerrariaMessageReceived(this, new TCRPlayer()
				{
					PlayerId = senderPlayerId,
					Name = Main.player[senderPlayerId].name
				}, message.Text);

			reader.BaseStream.Position = savedPosition;
			return orig(self, reader, senderPlayerId);
		}
	}
	public static class Extensions
	{
		public static TCRPlayer ToTCRPlayer(this Player player, int id)
		{
			return new TCRPlayer()
			{
				PlayerId = id,
				Name = player.name
			};
		}
	}
}