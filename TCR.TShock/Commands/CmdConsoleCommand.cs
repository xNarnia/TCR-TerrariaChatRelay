using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;
using TShockAPI;

namespace TCRTShock.Commands
{
	[Command]
	public class CmdConsoleCommand : ICommand
	{
		public string Name { get; } = "Console Command";

		public string CommandKey { get; } = "cmd";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Run any command as if you were on the server console!";

		public string Usage { get; } = "cmd ConsoleCommand ConsoleCommandParameters LIKE cmd time noon";

		public TerrariaChatRelay.Command.Permission DefaultPermissionLevel { get; } = TerrariaChatRelay.Command.Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = $"/{input}";
			TShockAPI.Commands.HandleCommand(new ConsoleRunner(), input);
			return "";
		}
	}

	public class ConsoleRunner : TSPlayer
	{
		public ConsoleRunner(string player = "Server") : base(player) 
			=> Group = new SuperAdminGroup();

		public override void SendMessage(string msg, byte red, byte green, byte blue)
			=> TerrariaChatRelay.Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, msg);
	}
}