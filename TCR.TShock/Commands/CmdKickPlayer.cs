using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdKickPlayer : ICommand
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
					input = input.Remove(0, Main.player[i].name.Length - 1);
					TShockAPI.TShock.Players[i].Kick(input, true,false, whoRanCommand.Username, true);
					return "";
				}
			}
			return "Player not found.";
		}
	}
}
