using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria;
using Terraria.Localization;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.TMLHooks
{
	// This is a GlobalNPC instead of a TCRHook, but it accomplishes the same goal.
	public class HookBosses : GlobalNPC
	{
		public static List<string> SpawnedBosses = new List<string>();

		public override void OnSpawn(NPC npc, IEntitySource source)
		{
			base.OnSpawn(npc, source);

			if (npc.boss)
			{
				try
				{
					if (!SpawnedBosses.Contains(npc.FullName))
						SpawnedBosses.Add(npc.FullName);
				}
				catch (Exception e)
				{
					PrettyPrint.Log("TerrariaChatRelay", "Error HookBosses: " + e.Message);
				}
				
				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, string.Format(Language.GetTextValue("Announcement.HasAwoken"), npc.FullName), TerrariaChatSource.BossSpawned);
			}
		}

		public override void OnKill(NPC npc)
		{
			base.OnKill(npc);

			if (npc.boss)
			{
				try
				{
					if (!SpawnedBosses.Contains(npc.FullName))
						SpawnedBosses.Add(npc.FullName);
				}
				catch (Exception e)
				{
					PrettyPrint.Log("TerrariaChatRelay", "Error HookBosses: " + e.Message);
				}

				Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, string.Format(Language.GetTextValue("Announcement.HasBeenDefeated_Single"), npc.FullName), TerrariaChatSource.BossKilled);
			}
		}
	}
}
