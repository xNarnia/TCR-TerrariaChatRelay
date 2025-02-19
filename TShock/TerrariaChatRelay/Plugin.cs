using Microsoft.Xna.Framework;
using OTAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.Net;
using Terraria.UI.Chat;
using TerrariaApi.Server;
using TerrariaChatRelay.Helpers;
using TShockAPI;
using TShockAPI.Hooks;

namespace TerrariaChatRelay
{
	[ApiVersion(2, 1)]
	public class Plugin : TerrariaPlugin
	{
		public override string Name => "TerrariaChatRelay";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public override string Author => "Narnia";

		public override string Description => "A chat redirecting plugin to send chat to your favorite messaging platforms.";

		public Version LatestVersion = new Version("0.0.0.0");

		public string CommandPrefix;
		public string SilentCommandPrefix;

		// Really weird way to fix the double broadcasting issue. TShock's code simply will not allow any other way.
		public List<Chatter> ChatHolder = new List<Chatter>();
		public static List<string> SpawnedBosses = new List<string>();

		public class Chatter
		{
			public TCRPlayer Player;
			public string Text;
			public Chatter()
			{

			}
		}

		public Plugin(Main game) : base(game)
		{

		}

		public override void Initialize()
		{
			Global.SavePath = Path.Combine(Directory.GetCurrentDirectory(), TShock.SavePath);
			Global.ModConfigPath = Path.Combine(Directory.GetCurrentDirectory(), TShock.SavePath, "TerrariaChatRelay");
			Global.Config = new TCRConfig().GetOrCreateConfiguration();

			if(Global.Config.LangOnlyForTShock != null)
				LanguageManager.Instance.SetLanguage(Global.Config.LangOnlyForTShock);

			CommandPrefix = TShock.Config.Settings.CommandSpecifier;
			SilentCommandPrefix = TShock.Config.Settings.CommandSilentSpecifier;

			ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

			// Add subscribers to list
			Core.Initialize(new Adapter());
			Core.ConnectClients();

			if (Global.Config.CheckForLatestVersion)
				Task.Run(GetLatestVersionNumber);

            //Hook into the chat. This specific hook catches the chat before it is sent out to other clients.
            //This allows us to edit the chat message before others get it.

            if (Global.Config.ShowChatMessages)
                ServerApi.Hooks.ServerChat.Register(this, OnChatReceived);
            if (Global.Config.ShowGameEvents)
				ServerApi.Hooks.ServerBroadcast.Register(this, OnServerBroadcast);

			if (Global.Config.ShowServerStartMessage)
				ServerApi.Hooks.GamePostInitialize.Register(this, OnServerStart);

			ServerApi.Hooks.NetGreetPlayer.Register(this, OnPlayerGreet);
			ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
			ServerApi.Hooks.NpcSpawn.Register(this, OnNPCSpawn);
			ServerApi.Hooks.NpcKilled.Register(this, NPC_Killed);

			//This hook is a part of TShock and not a part of TS-API. There is a strict distinction between those two assemblies.
			//This event is provided through the C# ``event`` keyword.
			GeneralHooks.ReloadEvent += OnReload;

			((CommandService)Core.CommandServ).ScanForCommands(this);
		}

		private void OnReload(ReloadEventArgs reloadEventArgs)
        {
            Global.Config = new TCRConfig().GetOrCreateConfiguration();
            CommandPrefix = TShock.Config.Settings.CommandSpecifier;
            SilentCommandPrefix = TShock.Config.Settings.CommandSilentSpecifier;

            Core.DisconnectClients();
			Core.ConnectClients();
		}

        private void OnChatReceived(ServerChatEventArgs args)
        {
            string text = args.Text;

            // Terraria's client side commands remove the command prefix, 
            // which results in arguments of that command show up on the Discord.
            // Thus, it needs to be reversed
            foreach (var item in ChatManager.Commands._localizedCommands)
            {
                if (item.Value._name == args.CommandId._name)
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = item.Key.Value + ' ' + text;
                    }
                    else
                    {
                        text = item.Key.Value;
                    }
                    break;
                }
            }

            if (text.StartsWith(CommandPrefix) || text.StartsWith(SilentCommandPrefix))
                return;

            if (text == "" || text == null)
                return;

            if (TShock.Players[args.Who].mute == true)
                return;

			if (!TShock.Players[args.Who].HasPermission(Permissions.canchat))
				return;

			var snippets = ChatManager.ParseMessage(text, Color.White);

            string outmsg = "";
            foreach (var snippet in snippets)
            {
                outmsg += snippet.Text;
            }

            ChatHolder.Add(new Chatter()
            {
                Player = Main.player[args.Who].ToTCRPlayer(args.Who),
                Text = $"{outmsg}"
            });

            Core.RaiseTerrariaMessageReceived(this, Main.player[args.Who].ToTCRPlayer(args.Who), text, TerrariaChatSource.PlayerChat);
        }

        private void OnPlayerGreet(GreetPlayerEventArgs args)
		{
			try
			{
				var player = Main.player[args.Who];
				// -1 is hacky, but works

				NetPacket packet =
					Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(
						NetworkText.FromFormattable("[TerrariaChatRelay]"), Color.LawnGreen, byte.MaxValue);
				NetManager.Instance.SendToClient(packet, args.Who);

				Core.RaiseTerrariaMessageReceived(this, Main.player[args.Who].ToTCRPlayer(-1), $"{Main.player[args.Who].name} has joined.", TerrariaChatSource.PlayerEnter);
			}
			catch (Exception)
			{
				PrettyPrint.Log("OnServerJoin could not be broadcasted.");
			}
		}

		private void OnServerLeave(LeaveEventArgs args)
		{
			try
			{
				if (Main.player[args.Who].name != ""
					&& Main.player[args.Who].name != " "
					&& Main.player[args.Who].name != null
					&& Main.player[args.Who].name.Replace("*", "") != ""
					&& Netplay.Clients[args.Who].State >= 3)
				{
					var player = Main.player[args.Who];
					// -1 is hacky, but works
					Core.RaiseTerrariaMessageReceived(this, player.ToTCRPlayer(-1), $"{player.name} has left.", TerrariaChatSource.PlayerLeave);
				}
			}
			catch (Exception)
			{
				PrettyPrint.Log("OnServerLeave could not be broadcasted.");
			}
		}

		private void OnNPCSpawn(NpcSpawnEventArgs args)
		{
			NPC npc = Main.npc[args.NpcId];

			try
			{
				if (!SpawnedBosses.Contains(npc.FullName))
					SpawnedBosses.Add(npc.FullName);
			}
			catch (Exception e)
			{
				PrettyPrint.Log("TerrariaChatRelay", "Error HookBosses: " + e.Message);
			}

			if (npc.boss)
				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, string.Format(Language.GetTextValue("Announcement.HasAwoken"), npc.FullName), TerrariaChatSource.BossSpawned);
		}

		private void NPC_Killed(NpcKilledEventArgs args)
		{
			NPC npc = Main.npc[args.npc.entityId];

			try
			{
				if (!SpawnedBosses.Contains(npc.FullName))
					SpawnedBosses.Add(npc.FullName);
			}
			catch (Exception e)
			{
				PrettyPrint.Log("TerrariaChatRelay", "Error HookBosses: " + e.Message);
			}

			if (npc.boss)
				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, string.Format(Language.GetTextValue("Announcement.HasBeenDefeated_Single"), npc.FullName), TerrariaChatSource.BossKilled);
		}

		private void OnServerStart(EventArgs args)
		{
			Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, Language.GetTextValue("LegacyMenu.8"), TerrariaChatSource.ServerStart);
		}

		private void OnServerStop()
		{
			Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, Language.GetTextValue("Net.ServerSavingOnExit"), TerrariaChatSource.ServerStop);
		}

		private void OnServerBroadcast(ServerBroadcastEventArgs args)
		{
			var literalText = Language.GetText(args.Message._text).Value;

			if (args.Message._substitutions?.Length > 0)
				literalText = string.Format(literalText, args.Message._substitutions);

			if (
				literalText.EndsWith(" has joined.") || // User joined
				literalText.EndsWith(" has left.") || // User left
				literalText.EndsWith(" has awoken!") //|| // Boss Spawn
													 //Regex.IsMatch(literalText, @".*?:\s+.*") // Chat
				)
				return;

			if (SpawnedBosses.Any(literalText.Contains))
				return;

				var CheckChat = ChatHolder.Where(x => literalText.Contains(x.Player.Name) && literalText.Contains(x.Text));
			if (CheckChat.Count() > 0)
			{
				ChatHolder.Remove(ChatHolder.First());
				return;
			}

			Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, literalText, TerrariaChatSource.ServerBroadcast);
		}

		//private void OnBroadcastMessage(NetworkText text, ref Color color, ref int ignorePlayer)
		//{
		//	var literalText = Language.GetText(text._text).Value;
		//	TCRCore.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, string.Format(literalText, text._substitutions));
		//}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Global.Config.ShowServerStartMessage)
					OnServerStop();

				if (Global.Config.ShowChatMessages)
					ServerApi.Hooks.ServerChat.Deregister(this, OnChatReceived);

				if (Global.Config.ShowGameEvents)
					ServerApi.Hooks.ServerBroadcast.Deregister(this, OnServerBroadcast);

				if (Global.Config.ShowServerStartMessage)
					ServerApi.Hooks.GamePostInitialize.Deregister(this, OnServerStart);

				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnPlayerGreet);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
				ServerApi.Hooks.NpcSpawn.Deregister(this, OnNPCSpawn);
				GeneralHooks.ReloadEvent -= OnReload;
			}
			base.Dispose(disposing);
		}


		/// <summary>
		/// <para>Loads the build.txt from GitHub to check if there is a newer version of TCR available. </para>
		/// If there is, a message will be displayed on the console and prepare a message for sending when the world is loading.
		/// </summary>
		public async Task GetLatestVersionNumber()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls12;
			var http = HttpWebRequest.CreateHttp("https://raw.githubusercontent.com/xNarnia/TCR-TerrariaChatRelay/master/version.txt");

			WebResponse res = null;
			try
			{
				res = await http.GetResponseAsync();
			}
			catch (Exception e)
			{
				PrettyPrint.Log("Error checking for version update: " + e.Message, ConsoleColor.Red);
				return;
			}

			using (StreamReader sr = new StreamReader(res.GetResponseStream()))
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
								PrettyPrint.Log($"A new version of TCR is available: V.{LatestVersion.ToString()}", ConsoleColor.Green);

							line = null;
						}
					}
					while (line != null);
				}
			}
		}
	}

	public static class Extensions
	{
		public static TCRPlayer ToTCRPlayer(this Player player, int id)
		{
			return new TCRPlayer()
			{
				PlayerId = id,
				Name = player.name,
				GroupPrefix = (id >= 0 ? TShock.Players[id].Group.Prefix : null),
				GroupSuffix = (id >= 0 ? TShock.Players[id].Group.Suffix : null)
			};
		}
	}
}
