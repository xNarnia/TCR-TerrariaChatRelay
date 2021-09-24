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
	public class CmdAddAdmin : ICommand
	{
		public string Name { get; } = "Add Admin";

		public string CommandKey { get; } = "addadmin";

		public string Description { get; } = "Grants the user access to Administrator level commands.";

		public Permission DefaultPermissionLevel { get; } = Permission.Owner;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.Replace("<@", "");
			input = input.Replace("!", "");
			input = input.Replace(">", "");

			if (ulong.TryParse(input, out ulong userId))
			{
				Main.Config.AdminUserIds.Add(userId);
				return "User successfully added.";
			}
			else
			{
				return "Could not parse User ID.";
			}
		}
	}
}
