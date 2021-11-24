using Microsoft.Xna.Framework;
using OTAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.Net;
using Terraria.UI.Chat;
using TerrariaApi.Server;
using TerrariaChatRelay;
using TerrariaChatRelay.Helpers;
using TShockAPI;
using TShockAPI.Hooks;

namespace TCRTShock
{
	[ApiVersion(2, 1)]
	public class TerrariaChatRelayTShock : TerrariaPlugin
	{
		public override string Name => "TerrariaChatRelay";

		public override Version Version => new Version(1, 0, 3, 2);

		public override string Author => "Panini";

		public override string Description => "A chat redirecting plugin to send chat to your favorite messaging platforms.";

		public Version LatestVersion = new Version("0.0.0.0");

		public string CommandPrefix;

		// Really weird way to fix the double broadcasting issue. TShock's code simply will not allow any other way.
		public List<Chatter> ChatHolder = new List<Chatter>();

		public class Chatter
		{
			public TCRPlayer Player;
			public string Text;
			public Chatter()
			{
				
			}
		}

		public List<int> BossIDs = new List<int>()
		{
			50, // King Slime
			4, // Eye of Cthulu			
			222, // Queen Bee
			13, // Eater of Worlds	
			266, // Brain of Cthulu
			35, // Skeletron
			113, // Wall of Flesh
			125, // Retinazer
			127, // Skeletron Prime	
			134, // The Destroyer
			262, // Plantera
			245, // Golem
			636, // Empress Of Light
			370, // Duke Fishron
			439, // Lunatic Cultist
			396 // Moon Lord
		};

		public TerrariaChatRelayTShock(Main game) : base(game)
		{

		}

		public override void Initialize()
		{
			Global.SavePath = Path.Combine(Directory.GetCurrentDirectory(), TShock.SavePath);
			Global.ModConfigPath = Path.Combine(Directory.GetCurrentDirectory(), TShock.SavePath, "TerrariaChatRelay");
			Global.Config = (TCRConfig)new TCRConfig().GetOrCreateConfiguration();

			CommandPrefix = TShock.Config.Settings.CommandSpecifier;

			ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

			Global.Config = (TCRConfig)new TCRConfig().GetOrCreateConfiguration();
			// Add subscribers to list
			Core.Initialize(new TShockAdapter());

			// Clients auto subscribe to list.
			//foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			//{
			//	var OnLoadConfigAssemblies = asm.GetTypes()
			//		.Where(type => !type.IsAbstract  && type.IsSubclassOf(typeof(TCRPlugin)));

			//	if (OnLoadConfigAssemblies.Count() > 0)
			//	{
			//		foreach (Type type in OnLoadConfigAssemblies)
			//		{
			//			// Get the constructor and create an instance of Config
			//			ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
			//			TCRPlugin plugin = (TCRPlugin)constructor.Invoke(new object[] { });
			//			plugin.Init(TerrariaChatRelay.Subscribers);
			//		}
			//	}
			//}

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
				Hooks.Game.PostInitialize += OnServerStart;

			ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
			ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
			ServerApi.Hooks.NpcSpawn.Register(this, OnNPCSpawn);

			//This hook is a part of TShock and not a part of TS-API. There is a strict distinction between those two assemblies.
			//This event is provided through the C# ``event`` keyword, which is a feature of the language itself.
			GeneralHooks.ReloadEvent += OnReload;

			((CommandService)Core.CommandServ).ScanForCommands(this);
		}

		private void OnReload(ReloadEventArgs reloadEventArgs)
		{
			Core.DisconnectClients();
			Global.Config = null;

			new TCRDiscord.Main();

			Core.ConnectClients();
		}

		private void OnChatReceived(ServerChatEventArgs args)
		{
			if (args.Text.StartsWith(CommandPrefix) || args.Text.StartsWith("."))
				return;

			if (args.Text == "" || args.Text == null)
				return;

			if (TShock.Players[args.Who].mute == true)
				return;

			var snippets = ChatManager.ParseMessage(args.Text, Color.White);

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


			Core.RaiseTerrariaMessageReceived(this, Main.player[args.Who].ToTCRPlayer(args.Who), args.Text);
		}

		private void OnServerJoin(JoinEventArgs args)
		{
			try
			{
				var player = Main.player[args.Who];
				// -1 is hacky, but works

				NetPacket packet =
					Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(
						NetworkText.FromFormattable("This chat is powered by TerrariaChatRelay."), Color.LawnGreen, byte.MaxValue);
				NetManager.Instance.SendToClient(packet, args.Who);

				Core.RaiseTerrariaMessageReceived(this, Main.player[args.Who].ToTCRPlayer(-1), $"{Main.player[args.Who].name} has joined.");
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
				if(Main.player[args.Who].name != ""
					&& Main.player[args.Who].name != " "
					&& Main.player[args.Who].name != null
					&& Main.player[args.Who].name.Replace("*" , "") != "")
				{
					var player = Main.player[args.Who];
					// -1 is hacky, but works
					Core.RaiseTerrariaMessageReceived(this, player.ToTCRPlayer(-1), $"{player.name} has left.");
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
			
			if (BossIDs.Contains(npc.netID))
			{
				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, $"{npc.FullName} has awoken!");
			}
		}

		private void OnServerStart()
		{
			Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, "The server is starting!");
		}

		private void OnServerStop()
		{
			Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, "The server is stopping!");
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

			var CheckChat = ChatHolder.Where(x => literalText.Contains(x.Player.Name) && literalText.Contains(x.Text));
			if (CheckChat.Count() > 0)
			{
				ChatHolder.Remove(ChatHolder.First());
				return;
			}

				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, literalText);
		}

		//private void OnBroadcastMessage(NetworkText text, ref Color color, ref int ignorePlayer)
		//{
		//	var literalText = Language.GetText(text._text).Value;
		//	TerrariaChatRelay.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, string.Format(literalText, text._substitutions));
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
					Hooks.Game.PostInitialize -= OnServerStart;

				ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
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
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			var http = HttpWebRequest.CreateHttp("https://raw.githubusercontent.com/xPanini/TCR-TerrariaChatRelay-TShock/master/version.txt");

			WebResponse res = null;
			try
			{
				res = await http.GetResponseAsync();
			}
			catch (Exception e)
			{
				PrettyPrint.WriteLine(e.Message);
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
								PrettyPrint.Log($"A new version of TCR is available: V.{LatestVersion.ToString()}");

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
