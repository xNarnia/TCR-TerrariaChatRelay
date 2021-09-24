using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdKillPlayer : ICommand
	{
		public string Name { get; } = "Kill Player";

		public string CommandKey { get; } = "kill";

		public string Description { get; } = "Kills the designated player >:)";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			if(input == null || input == "")
				return "Please designate a player to kill. Example: \"kill Unlucky Player\"";

			for(var i = 0; i < Main.player.Length; i++)
			{
				if(Main.player[i].name == input)
				{
					NetMessage.SendPlayerDeath(i, PlayerDeathReason.LegacyDefault(), 99999, (new Random()).Next(-1, 1), false, -1, -1);
					return "";
				}
			}
			return "Player not found!";
		}
	}
}
