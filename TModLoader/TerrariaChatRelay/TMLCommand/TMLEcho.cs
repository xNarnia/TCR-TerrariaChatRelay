using System;
using Terraria.ModLoader;

namespace TerrariaChatRelay.TMLCommand
{
	public class TMLEcho : ModCommand
	{
		public override CommandType Type
			=> CommandType.Chat;

		// The desired text to trigger this command
		public override string Command
			=> "echo";

		// A short usage explanation for this command
		public override string Usage
			=> "echo";

		// A short description of this command
		public override string Description
			=> "echo.";

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			Console.WriteLine(input);
		}
	}
}
