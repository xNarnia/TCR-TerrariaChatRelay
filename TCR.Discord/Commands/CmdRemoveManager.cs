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
	public class CmdRemoveManager : ICommand
	{
		public string Name { get; } = "Remove Manager";

		public string CommandKey { get; } = "removemanager";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Removes the user's access to Manager level commands.";

		public string Usage { get; } = "removemanager @DiscordUser";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.Replace("<@", "");
			input = input.Replace("!", "");
			input = input.Replace(">", "");

			if (ulong.TryParse(input, out ulong userId))
			{
				if (Main.Config.ManagerUserIds.Contains(userId))
				{
					Main.Config.AdminUserIds.Remove(userId);
					Main.Config.SaveJson();
					return "User successfully deleted.";
				}
				else
				{
					return "Could not find user in admin database.";
				}
			}
			else
			{
				return "Could not find user. Example: removeadmin @UserToRemovePermissions";
			}
		}
	}
}
