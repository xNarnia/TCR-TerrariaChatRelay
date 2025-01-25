using System;
using Terraria;
using Terraria.DataStructures;

namespace TerrariaChatRelay.TCRCommand.Commands
{
	[Command]
	public class CmdKillPlayer : ICommand
	{
		public string Name { get; } = "Kill Player";

		public string CommandKey { get; } = "kill";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Kills the designated player >:)";

		public string Usage { get; } = "kill PlayerName";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
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
