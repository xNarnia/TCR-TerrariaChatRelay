using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.UI.Chat;
using TerrariaChatRelay;
using Microsoft.Xna.Framework;
using TerrariaChatRelay.Helpers;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;
using System.Net.Http;

namespace TerrariaChatRelay
{
	public class TerrariaChatRelay : Mod
	{
		public Version LatestVersion = new Version("0.0.0.0");
		public string PlayerJoinEndingString;
		public string PlayerLeaveEndingString;

		public TerrariaChatRelay()
		{
		}

		public override void Load()
		{
			base.Load();

			ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			Global.Config = (TCRConfig)new TCRConfig().GetOrCreateConfiguration();

			// Intercept DeserializeAsServer method
			//NetTextModule.DeserializeAsServer += NetTextModule_DeserializeAsServer;

			On_ChatHelper.BroadcastChatMessage += BroadcastChatMessage;
			On_WorldGen.serverLoadWorld += OnWorldLoadStart;
			On_Netplay.StopListening += OnServerStop;
			On_NetMessage.SyncConnectedPlayer += OnPlayerJoin_NetMessage_SyncConnectedPlayer;
			On_RemoteClient.Reset += RemoteClient_Reset;
			On_NetMessage.greetPlayer += NetMessage_greetPlayer;

			PlayerJoinEndingString = Language.GetText("LegacyMultiplayer.19").Value.Split(new string[] { "{0}" }, StringSplitOptions.None).Last();
			PlayerLeaveEndingString = Language.GetText("LegacyMultiplayer.20").Value.Split(new string[] { "{0}" }, StringSplitOptions.None).Last();

			if (ModLoader.TryGetMod("NoMoreTombs", out Mod NoMoreTombs))
			{
				PrettyPrint.Log("[NoMoreTombs] Incompatibility : Death messages can not be routed.", ConsoleColor.Red);
			}

			// Add subscribers to list
			Core.Initialize(new tModLoaderAdapter());

			((CommandService)Core.CommandServ).ScanForCommands(this);

			Core.ConnectClients();

			if (Global.Config.CheckForLatestVersion)
				Task.Run(GetLatestVersionNumber);
		}

		private void NetMessage_greetPlayer(On_NetMessage.orig_greetPlayer orig, int plr)
		{
			NetPacket packet = Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("This chat is powered by TerrariaChatRelay"), Color.LawnGreen, byte.MaxValue);
			NetManager.Instance.SendToClient(packet, plr);
			orig(plr);
		}

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
				res = await client.GetAsync("https://raw.githubusercontent.com/xPanini/TCR-TerrariaChatRelay/master/TCR-TModLoader/TerrariaChatRelay/build.txt");
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
								PrettyPrint.Log($"A new version of TCR is available: V.{LatestVersion.ToString()}");

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
			//NetTextModule.DeserializeAsServer -= NetTextModule_DeserializeAsServer;
			On_ChatHelper.BroadcastChatMessage -= BroadcastChatMessage;
			Global.Config = null;
		}

		/// <summary>
		/// Hooks onto the World Load method to send a message when the server is starting.
		/// </summary>
		private Task OnWorldLoadStart(On_WorldGen.orig_serverLoadWorld orig)
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
				PrettyPrint.Log("Error checking for version update: " + e.Message, ConsoleColor.Red);
			}

			return orig();
		}

		/// <summary>
		/// Hooks onto the StopListening method to send a message when the server is stopping.
		/// </summary>
		private void OnServerStop(On_Netplay.orig_StopListening orig)
		{
			if (Global.Config.ShowServerStopMessage)
				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, "The server is stopping!");

			orig();
		}

		/// <summary>
		/// Intercept all other messages from Terraria. E.g. blood moon, death notifications, and player join/leaves.
		/// </summary>
		private void BroadcastChatMessage(On_ChatHelper.orig_BroadcastChatMessage orig, NetworkText text, Color color, int excludedPlayer)
		{
			if (Global.Config.ShowGameEvents && !text.ToString().EndsWith(PlayerJoinEndingString) && !text.ToString().EndsWith(PlayerLeaveEndingString))
				Core.RaiseTerrariaMessageReceived(this, (excludedPlayer > 0 ? Main.player[excludedPlayer].ToTCRPlayer(excludedPlayer) : TCRPlayer.Server), text.ToString());

			orig(text, color, excludedPlayer);
		}

		/// <summary>
		/// Intercept chat messages sent from players.
		/// </summary>
		//private bool NetTextModule_DeserializeAsServer(NetTextModule.orig_DeserializeAsServer orig, Terraria.GameContent.NetModules.NetTextModule self, BinaryReader reader, int senderPlayerId)
		//{
		//	long savedPosition = reader.BaseStream.Position;
		//	ChatMessage message = ChatMessage.Deserialize(reader);

		//	if (Global.Config.ShowChatMessages)
		//		Core.RaiseTerrariaMessageReceived(this, new TCRPlayer() { 
		//			PlayerId = senderPlayerId,
		//			Name = Main.player[senderPlayerId].name
		//		}, message.Text);

		//	reader.BaseStream.Position = savedPosition;
		//	return orig(self, reader, senderPlayerId);
		//}
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