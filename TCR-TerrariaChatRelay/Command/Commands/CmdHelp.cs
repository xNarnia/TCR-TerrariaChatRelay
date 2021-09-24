using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdHelp : ICommand
	{
		public string Name { get; } = "Help/Info";

		public string CommandKey { get; } = "help";

		public string Description { get; } = "Displays this help message!";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			return string.Join("</br>", 
				Core.CommandServ.Commands
					.Where(x => x.Value.DefaultPermissionLevel <= whoRanCommand.PermissionLevel)
					.Select(x => $"</b>{(x.Value.DefaultPermissionLevel == Permission.User ? "" : $"[{x.Value.DefaultPermissionLevel.ToString()}] - ")}{x.Value.CommandKey}</b> - {x.Value.Description}")
			);
		}
	}

	// Lazy alias until I can get around to adding aliases
	[Command]
	public class CmdInfo : CmdHelp, ICommand
	{
		public new string CommandKey { get; } = "info";
	}
}
