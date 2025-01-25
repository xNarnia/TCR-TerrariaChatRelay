using Terraria;

namespace TerrariaChatRelay.TCRCommand.Commands.Discord
{
	[Command]
	public class CmdKickManager : ICommand
	{
		public string Name { get; } = "Kick Player";

		public string CommandKey { get; } = "kick";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Kicks the specified player. (Careful not to trigger other Discord bots!)";

		public string Usage { get; } = "kick PlayerName";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.ToLower();
			if (input == null || input == "")
				return "Specify a player to kick. Example: \"kick AnOnlinePlayer\"";

			for (var i = 0; i < Main.player.Length; i++)
			{
				if (Main.player[i].name.ToLower() == input)
				{
					Main.ExecuteCommand("kick " + input, new TCRCommandCaller());
					return "";
				}
			}
			return "Player not found.";
		}
	}
}
