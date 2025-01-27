using Terraria.ModLoader;

namespace TerrariaChatRelay.TMLCommand
{
	public class TMLCmdReload : ModCommand
	{
		public override CommandType Type
			=> CommandType.Console;

		// The desired text to trigger this command
		public override string Command
			=> "tcrreload";

		// A short usage explanation for this command
		public override string Usage
			=> "tcrreload";

		// A short description of this command
		public override string Description
			=> "Reloads TerrariaChatRelay.";

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			Core.DisconnectClients();
			Core.ConnectClients();
		}
	}
}
