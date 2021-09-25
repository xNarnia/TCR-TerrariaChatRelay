using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCRDiscord;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;

namespace TCRDiscord.Commands
{
	[Command]
	public class CmdAddManager : ICommand
	{
		public string Name { get; } = "Add Manager";

		public string CommandKey { get; } = "addmanager";

		public string Description { get; } = "Grants the user access to Manager level commands.";

		public string Usage { get; } = "addmanager @DiscordUser";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.Replace("<@", "");
			input = input.Replace("!", "");
			input = input.Replace(">", "");

			if (ulong.TryParse(input, out ulong userId))
			{
				Main.Config.ManagerUserIds.Add(userId);
				return "User successfully added.";
			}
			else
			{
				return "Could not find user. Example: addadmin @UserToGivePermissions";
			}
		}
	}
}
