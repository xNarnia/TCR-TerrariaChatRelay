using Terraria;

namespace TerrariaChatRelay.TCRCommand.Commands.Discord
{
	[Command]
	public class CmdBanManager : ICommand
	{
		public string Name { get; } = "Ban Player";

		public string CommandKey { get; } = "ban";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Bans the specified player. (Careful not to trigger other Discord bots!)";

		public string Usage { get; } = "ban PlayerName";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.ToLower();
			if (input == null || input == "")
				return "Specify a player to ban. Example: \"ban AnOnlinePlayer\"";

			for (var i = 0; i < Main.player.Length; i++)
			{
				if (Main.player[i].name.ToLower() == input)
				{
					Main.ExecuteCommand("ban " + input, new TCRCommandCaller());
					return "";
				}
			}
			return "Player not found.";
		}
	}
}
