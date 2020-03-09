using Microsoft.Xna.Framework;
using OTAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TerrariaChatRelay.Helpers;
using TShockAPI;
using TShockAPI.Hooks;

namespace TerrariaChatRelay
{
	[ApiVersion(2, 1)]
	public class TerrariaChatRelay : TerrariaPlugin
	{
		public override string Name => "TerrariaChatRelay";

		public override Version Version => new Version(0, 9, 1);

		public override string Author => "Panini";

		public override string Description => "A chat redirecting plugin to send chat to your favorite messaging platforms.";

		public Version LatestVersion = new Version("0.0.0.0");

		public string CommandPrefix;

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
			370, // Duke Fishron
			439, // Lunatic Cultist
			396 // Moon Lord
		};

		public TerrariaChatRelay(Main game) : base(game)
		{

		}

		public override void Initialize()
		{
			Global.SavePath = Path.Combine(Directory.GetCurrentDirectory(), TShock.SavePath);
			Global.ModConfigPath = Path.Combine(Directory.GetCurrentDirectory(), TShock.SavePath, "TerrariaChatRelay");

			var config = ConfigFile.Read(Path.Combine(Global.SavePath, "config.json"));
			CommandPrefix = config.CommandSpecifier;

			ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

			Global.Config = (TCRConfig)new TCRConfig().GetOrCreateConfiguration();
			// Add subscribers to list
			EventManager.Initialize();

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
			//			plugin.Init(EventManager.Subscribers);
			//		}
			//	}
			//}
			new DiscordChatRelay.Main();

			EventManager.ConnectClients();

			if (Global.Config.CheckForLatestVersion)
				Task.Run(GetLatestVersionNumber);

			//Hook into the chat. This specific hook catches the chat before it is sent out to other clients.
			//This allows us to edit the chat message before others get it.
			ServerApi.Hooks.ServerChat.Register(this, OnChatReceived);
			ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
			ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
			ServerApi.Hooks.NpcSpawn.Register(this, OnNPCSpawn);
			ServerApi.Hooks.ServerBroadcast.Register(this, OnServerBroadcast);
			Hooks.Game.PostInitialize += OnServerStart;
			//World event

			//This hook is a part of TShock and not a part of TS-API. There is a strict distinction between those two assemblies.
			//This event is provided through the C# ``event`` keyword, which is a feature of the language itself.
			GeneralHooks.ReloadEvent += OnReload;
		}

		private void OnReload(ReloadEventArgs reloadEventArgs)
		{
			EventManager.DisconnectClients();
			Global.Config = null;
		}

		private void OnChatReceived(ServerChatEventArgs args)
		{
			if (args.Text.StartsWith(CommandPrefix))
				return;

			EventManager.RaiseTerrariaMessageReceived(this, args.Who, args.Text);
		}

		private void OnServerJoin(JoinEventArgs args)
		{
			try
			{
				EventManager.RaiseTerrariaMessageReceived(this, -1, $"{Main.player[args.Who].name} has joined.");
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
				EventManager.RaiseTerrariaMessageReceived(this, -1, $"{Main.player[args.Who].name} has left.");
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
				EventManager.RaiseTerrariaMessageReceived(this, -1, $"{npc.FullName} has awoken!");
			}
		}

		private void OnServerStart()
		{
			EventManager.RaiseTerrariaMessageReceived(this, -1, "The server is starting!");
		}

		private void OnServerStop()
		{
			EventManager.RaiseTerrariaMessageReceived(this, -1, "The server is stopping!");
		}

		private void OnServerBroadcast(ServerBroadcastEventArgs args)
		{
			var literalText = Language.GetText(args.Message._text).Value;
			EventManager.RaiseTerrariaMessageReceived(this, -1, string.Format(literalText, args.Message._substitutions));
		}

		//private void OnBroadcastMessage(NetworkText text, ref Color color, ref int ignorePlayer)
		//{
		//	var literalText = Language.GetText(text._text).Value;
		//	EventManager.RaiseTerrariaMessageReceived(this, -1, string.Format(literalText, text._substitutions));
		//}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				OnServerStop();
				ServerApi.Hooks.ServerChat.Deregister(this, OnChatReceived);
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
			var http = HttpWebRequest.CreateHttp("https://raw.githubusercontent.com/xPanini/TCR-TerrariaChatRelay/master/build.txt");

			WebResponse res = null;
			try
			{
				res = await http.GetResponseAsync();
			}
			catch (Exception e)
			{
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
}
