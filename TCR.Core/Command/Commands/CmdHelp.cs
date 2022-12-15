using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRCore.Command.Commands
{
	[Command]
	public class CmdHelp : ICommand
	{
		public string Name { get; } = "Help/Info";

		public string CommandKey { get; } = "help";

		public string[] Aliases { get; } = { "info" };

		public string Description { get; } = "Displays this help message!";

		public string Usage { get; } = "help, help manager, help admin, help owner, help all";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			Permission permissionRequested;

			switch (input.ToLower())
			{
				case "manager":
					permissionRequested = Permission.Manager;
					break;
				case "admin":
					permissionRequested = Permission.Admin;
					break;
				case "owner":
					permissionRequested = Permission.Owner;
					break;
				case "all":
					permissionRequested = whoRanCommand.PermissionLevel;
					break;
				default:
					permissionRequested = Permission.User;
					break;
			}

			if (permissionRequested > whoRanCommand.PermissionLevel)
				return "</b>You don't have permission to use this command!</b>";

			var commands = Core.CommandServ.Commands
					.Where(x => x.Value.DefaultPermissionLevel <= whoRanCommand.PermissionLevel && (x.Value.DefaultPermissionLevel == permissionRequested || input.ToLower() == "all"))
					.Where(x => !x.Value.Aliases.Contains(x.Key))
					.OrderBy(x => x.Value.DefaultPermissionLevel);

			var helpList = new List<string>();
			string output = "";

			foreach (var command in commands)
			{
				// First line
				output += "</br></br></quote></b>";

				if (command.Value.DefaultPermissionLevel != Permission.User)
				{
					output += $"[{command.Value.DefaultPermissionLevel}] - ";
				}

				output += $"{command.Value.CommandKey}</b> - {command.Value.Description}</br>";

				// Next line, show aliases if available
				if (command.Value.Aliases.Count() > 0)
				{
					var aliases = command.Value.Aliases
						.Select(x => $"</code>{x}</code>");

					output += $"</quote>     </b>Aliases: </b>{string.Join(", ", aliases)}</br>";
				}

				// Last line
				if (command.Value.Usage != null && command.Value.Usage != "")
				{
					output += $"</quote>     </b>Example:</b> </code>{command.Value.Usage}</code>";
				}
			}

			return output;
		}
	}
}
