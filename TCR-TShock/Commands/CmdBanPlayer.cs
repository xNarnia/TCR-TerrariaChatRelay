using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace TerrariaChatRelay.Command.Commands
{
	// [Command]
	public class CmdBanPlayer : ICommand
	{
		public string Name { get; } = "Ban Player";

		public string CommandKey { get; } = "ban";

		public string Description { get; } = "Bans the specified player. (Careful not to trigger other Discord bots!)";

		public string Usage { get; } = "ban PlayerName";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.ToLower();
			if (input == null || input == "")
				return "Specify a player to ban. Example: \"ban AnOnlinePlayer\"";

			for (var i = 0; i < Main.player.Length; i++)
			{
				if (Main.player[i].name.ToLower() == input)
				{
					input = input.Remove(0, Main.player[i].name.Length - 1);
					TShockAPI.TShock.Players[i].Ban(input);

					return "";
				}
			}
			return "Player not found.";
		}
	}
}
