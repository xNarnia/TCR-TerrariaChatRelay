using System.Linq;
using System.Collections.Generic;

namespace TerrariaChatRelay.TCRCommand.Commands
{
	[Command]
	public class CmdPlayersOnline : ICommand
	{
		public string Name { get; } = "Players Online";

		public string CommandKey { get; } = "playing";

		public string[] Aliases { get; } = { "online", "who", "players", "active" };

		public string Description { get; } = "Displays the list of players online";

		public string Usage { get; } = "playing";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			var players = Terraria.Main.player.Where(x => x.name.Length != 0);
			List<string> teamEmojis = new List<string> {"", "🟥", "🟩", "🟦", "🟨", "🟪"};

			if (players.Count() == 0)
			{
				return $"</b>Players Online:</b> {players.Count()} / {Terraria.Main.maxNetPlayers}</br></box>No players online!</box>";
			}
			return $"</b>Players Online:</b> {players.Count()} / {Terraria.Main.maxNetPlayers}" +
				"</br></box>" +
				string.Join(", ", players.Select(x =>
				{
					string emoji = x.team >= 0 && x.team < teamEmojis.Count ? teamEmojis[x.team] : "";
					return $"{emoji} {x.name} [HP: {x.statLife}/{x.statLifeMax2}]";
				})).Replace("`", "") +
				"</box>";
		}
	}
}
