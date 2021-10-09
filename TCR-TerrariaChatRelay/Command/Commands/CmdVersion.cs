using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdVersion : ICommand
	{
		public string Name { get; } = "TCR Version";

		public string CommandKey { get; } = "version";

		public string Description { get; } = "Displays the version of TCR!";

		public string Usage { get; } = "version";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			return "</b>TerrariaChatRelay Version:</b> " + typeof(Core).Assembly.GetName().Version.ToString();
		}
	}
}
