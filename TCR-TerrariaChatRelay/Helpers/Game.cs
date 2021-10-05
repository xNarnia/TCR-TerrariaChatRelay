using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay
{
	public static class Game
	{
		public static class World
		{
			public static string GetName()
				=> Terraria.Main.worldName;

			public static string GetEvilType()
				=> Terraria.WorldGen.crimson == false ? "Corruption" : "Crimson";

			public static string GetWorldSeed()
				=> Terraria.WorldGen.currentWorldSeed;

			public static string getWorldID()
				=> Terraria.Main.worldID.ToString();

			public static string getWorlSize()
				=> $"{Terraria.Main.maxTilesX}x{Terraria.Main.maxTilesY}";

			public static string getWorldPath()
				=> Terraria.Main.WorldPath;

			public static bool IsExpertMode()
				=> Terraria.Main.expertMode;

			public static bool IsMasterMode()
				=> Terraria.Main.masterMode;

			public static bool IsHardMode()
				=> Terraria.Main.hardMode;
		}

		public static string GetOnlinePlayers()
			 => string.Join(", ", Terraria.Main.player.Where(x => x.name.Length != 0).Select(x => x.name));
	}
}
