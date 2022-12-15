using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using TCRCore.Command;

namespace TCRCore.Commands
{
	[Command]
	public class CmdMods : ICommand
	{
		public string Name { get; } = "Show Mods";

		public string CommandKey { get; } = "mods";
		public string[] Aliases { get; } = { "modlist" };

		public string Description { get; } = "Displays current mods on the server";

		public string Usage { get; } = "mods";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			return $"</b>Mod List:</b></br></box>{string.Join(", ", ModLoader.Mods.Select(x => x.DisplayName).Where(x => x != "tModLoader"))}</box>";
		}
	}
}
