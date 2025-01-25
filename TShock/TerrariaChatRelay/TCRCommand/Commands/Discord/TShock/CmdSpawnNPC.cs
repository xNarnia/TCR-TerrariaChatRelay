using System;
using System.Linq;
using Terraria;
using TShockAPI;

namespace TerrariaChatRelay.TCRCommand.Commands
{
	[Command]
	public class CmdSpawnNPC : ICommand
	{
		public string Name { get; } = "Spawn NPC";

		public string CommandKey { get; } = "spawnmob";

		public string[] Aliases { get; } = { "sm" };

		public string Description { get; } = "Spawn NPC to designated player.";

		public string Usage { get; } = "spawnmob playername, npcname/id, amount";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;
		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.ToLower();

			String[] spearator = { ", " };
			Int32 count = 3;
			String[] parameters = input.Split(spearator, count, StringSplitOptions.RemoveEmptyEntries);

			if (parameters.Count() < 2)
				return "Specify a player Name to spawn NPC. Example: \"sm AnOnlinePlayer, npcname, amount\"";

			if (parameters[1] == null || parameters[1] == "")
				return "Specify a NPC ID or name. Example: \"sm AnOnlinePlayer, Wyvern, 2\"";

			for (var i = 0; i < Main.player.Length; i++)
			{
				if (Main.player[i].name.ToLower() == parameters[0])
				{
					var npcs = TShock.Utils.GetNPCByIdOrName(parameters[1]);
					if (npcs.Count == 0)
					{
						return "Invalid mob type!";
					}
					else if (npcs.Count > 1)
					{
						return "Matched with more than one npc!";
					}
					else
					{
						var npc = npcs[0];
						if (npc.type >= 1 && npc.type < Main.maxNPCs && npc.type != 113)
						{
							input = parameters[0].Remove(0, Main.player[i].name.Length - 1);

							if (int.TryParse(parameters[2], out int amount))
							{
								TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, TShock.Players[i].TileX, TShock.Players[i].TileY, 35, 10);
							}
							else
							{
								TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, 1, TShock.Players[i].TileX, TShock.Players[i].TileY, 35, 10);
							}
							return "Command Executed.";
						}
					}
				}
			}
			return "Player not found.";
		}
	}
}
