using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay.Command.Commands.Discord
{
	[Command]
	public class CmdListAdmin : ICommand
	{
		public string Name { get; } = "List Admin";

		public string CommandKey { get; } = "listadmin";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Lists all users given access to Administrator level commands.";

		public string Usage { get; } = "listadmin";

		public Permission DefaultPermissionLevel { get; } = Permission.Owner;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			return "**Administrators for TerrariaChatRelay:**\n" + string.Join("\n", DiscordPlugin.Config.AdminUserIds.Where(x => x != 0).Select(x => $"<@{x}> - ID: {x}\n"));
		}
	}
}
