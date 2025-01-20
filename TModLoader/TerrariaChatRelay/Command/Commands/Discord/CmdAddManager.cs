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
	public class CmdAddManager : ICommand
	{
		public string Name { get; } = "Add Manager";

		public string CommandKey { get; } = "addmanager";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Grants the user access to Manager level commands.";

		public string Usage { get; } = "addmanager @DiscordUser";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.Replace("<@", "");
			input = input.Replace("!", "");
			input = input.Replace(">", "");

			if (ulong.TryParse(input, out ulong userId))
			{
				DiscordPlugin.Config.ManagerUserIds.Add(userId);
				DiscordPlugin.Config.SaveJson();
				return "User successfully added.";
			}
			else
			{
				return "Could not find user. Example: addadmin @UserToGivePermissions";
			}
		}
	}
}
