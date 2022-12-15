using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRCore.Command.Commands
{
	[Command]
	public class CmdVersion : ICommand
	{
		public string Name { get; } = "TCR Version";

		public string CommandKey { get; } = "version";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Displays the version of TCR!";

		public string Usage { get; } = "version";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			string terraria = "";
#if TSHOCK
			terraria = "TShock";
#endif
#if TMODLOADER
			terraria = "tModLoader 1.4";
#endif

			return $"</b>TerrariaChatRelay Version:</b> {terraria} - v{Core.TCRVersion}";
		}
	}
}
