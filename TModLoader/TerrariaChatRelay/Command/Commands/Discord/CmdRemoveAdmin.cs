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
	public class CmdRemoveAdmin : ICommand
	{
		public string Name { get; } = "Remove Admin";

		public string CommandKey { get; } = "removeadmin";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Removes the user's access to Administrator level commands.";

		public string Usage { get; } = "removeadmin @DiscordUser";

		public Permission DefaultPermissionLevel { get; } = Permission.Owner;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.Replace("<@", "");
			input = input.Replace("!", "");
			input = input.Replace(">", "");

			if (ulong.TryParse(input, out ulong userId))
			{
				if (DiscordPlugin.Config.AdminUserIds.Contains(userId))
				{
					DiscordPlugin.Config.AdminUserIds.Remove(userId);
					DiscordPlugin.Config.SaveJson();
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
