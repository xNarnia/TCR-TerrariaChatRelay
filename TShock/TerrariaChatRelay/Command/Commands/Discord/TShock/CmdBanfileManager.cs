using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Helpers;
using TerrariaChatRelay.Clients.DiscordClient;
using Terraria;
using System.IO;

namespace TerrariaChatRelay.Command.Commands.Discord
{
	[Command]
	public class CmdBanfileManager : ICommand
	{
		public string Name { get; } = "Banfile.txt Location";

		public string CommandKey { get; } = "banfile";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Displays the location of the banfile.txt file on the system.";

		public string Usage { get; } = "banfile";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			var path = Netplay.BanFilePath;
			if(path == "banlist.txt")
			{
				path = Path.Combine(Directory.GetCurrentDirectory(), "banfile.txt");
			}

			return "Banfile location: " + path;
		}
	}
}
