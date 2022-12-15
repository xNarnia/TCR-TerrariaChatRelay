using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRCore
{
	public static class Game
	{
		public static class World
		{
			public static string GetName()
				=> Terraria.Main.worldName;

			public static string GetEvilType()
				=> Terraria.WorldGen.crimson == false ? "Corruption" : "Crimson";

#if TSHOCK
			public static string GetWorldSeed()
				=> Terraria.WorldGen.currentWorldSeed;
#endif

			public static string getWorldID()
				=> Terraria.Main.worldID.ToString();

			public static string getWorldSize()
				=> $"{Terraria.Main.maxTilesX}x{Terraria.Main.maxTilesY}";

			public static string getWorldPath()
				=> Terraria.Main.WorldPath;

			public static bool IsExpertMode()
				=> Terraria.Main.expertMode;

#if TSHOCK
			public static bool IsMasterMode()
				=> Terraria.Main.masterMode;
#endif

			public static bool IsHardMode()
				=> Terraria.Main.hardMode;
		}

		public static string GetOnlinePlayers()
			 => string.Join(", ", Terraria.Main.player.Where(x => x.name.Length != 0).Select(x => x.name));

		public static int GetCurrentPlayerCount()
			=> Terraria.Main.player.Where(x => x.name.Length != 0).Count();

		public static int GetMaxPlayerCount()
			=> Terraria.Main.maxNetPlayers;
	}
}
