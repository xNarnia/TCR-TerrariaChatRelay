﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdConsoleCommand : ICommand
	{
		public string Name { get; } = "Console Command";

		public string CommandKey { get; } = "cmd";

		public string[] Aliases { get; } = { "console" };

		public string Description { get; } = "Run any command as if you were on the server console!";

		public string Usage { get; } = "cmd ConsoleCommand ConsoleCommandParameters LIKE cmd time noon";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;


		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			Main.ExecuteCommand(input, new TCRCommandCaller());

			return "Running command...";
		}
	}
}