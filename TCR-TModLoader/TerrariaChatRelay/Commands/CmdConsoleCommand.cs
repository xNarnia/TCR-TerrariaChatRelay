using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay
{
	[Command]
	public class CmdConsoleCommand : ICommand
	{
		public string Name { get; } = "Console Command";

		public string CommandKey { get; } = "cmd";

		public string Description { get; } = "Run any command as if you were on the server console!";

		public string Usage { get; } = "cmd ConsoleCommand ConsoleCommandParameters LIKE cmd time noon";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			Main.ExecuteCommand(input, new TCRCommandCaller());
			return "Command executed.";
		}
	}

	public class TCRCommandCaller : CommandCaller
	{
		public CommandType CommandType => CommandType.Console;

		public Player Player => null;

		public void Reply(string text, Color color)
		{
			string[] array = text.Split('\n');
			foreach (string value in array)
			{
				if(value.Length > 0)
					Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, value);
			}
		}
	}
}