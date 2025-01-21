using System;
using System.Linq;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdPlayersOnline : ICommand
	{
		public string Name { get; } = "Players Online";

		public string CommandKey { get; } = "playing";

		public string[] Aliases { get; } = { "online", "who" };

		public string Description { get; } = "Displays the list of players online";

		public string Usage { get; } = "playing";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			var players = Terraria.Main.player.Where(x => x.name.Length != 0);
			if (players.Count() == 0)
			{
				return $"</b>Players Online:</b> {players.Count()} / {Terraria.Main.maxNetPlayers}</br></box>No players online!</box>";
			}
			return $"</b>Players Online:</b> {players.Count()} / {Terraria.Main.maxNetPlayers}" + "</br></box>" + string.Join(", ", players.Select(x => x.name)).Replace("`", "") + "</box>";
		}
	}
}
