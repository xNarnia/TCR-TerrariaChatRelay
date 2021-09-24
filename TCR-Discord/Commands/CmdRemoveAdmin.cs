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
	public class CmdRemoveAdmin : ICommand
	{
		public string Name { get; } = "Remove Admin";

		public string CommandKey { get; } = "removeadmin";

		public string Description { get; } = "Removes the user's access to Administrator level commands.";

		public Permission DefaultPermissionLevel { get; } = Permission.Owner;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.Replace("<@", "");
			input = input.Replace("!", "");
			input = input.Replace(">", "");

			if (ulong.TryParse(input, out ulong userId))
			{
				if (Main.Config.AdminUserIds.Contains(userId))
				{
					Main.Config.AdminUserIds.Remove(userId);
					return "User successfully deleted.";
				}
				else
				{
					return "Could not find user in admin database.";
				}
			}
			else
			{
				return "Could not parse User ID. Did you right-click & Copy Id from user?";
			}
		}
	}
}
