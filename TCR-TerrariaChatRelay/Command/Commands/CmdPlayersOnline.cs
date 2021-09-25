using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdPlayersOnline : ICommand
	{
		public string Name { get; } = "Player List";

		public string CommandKey { get; } = "playing";

		public string Description { get; } = "Displays the list of players online";

		public string Usage { get; } = "playing";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			var players = Terraria.Main.player.Where(x => x.name.Length != 0);
			if (players.Count() == 0)
			{
				return $"</b>Players Online:</b> {players.Count()} / {Terraria.Main.maxNetPlayers}</br></box>No players online!</box>";
			}
			return $"</b>Players Online:</b> {players.Count()} / {Terraria.Main.maxNetPlayers}" + "</br></box>" + string.Join(", ", players.Select(x => x.name)).Replace("`", "") + "</box>";
		}
	}

	// Lazy alias until I can get around to adding aliases
	[Command]
	public class CmdPlayersOnlineAlias : CmdPlayersOnline, ICommand
	{
		public new string CommandKey { get; } = "online";

		public new string Usage { get; } = "online";
	}
}
