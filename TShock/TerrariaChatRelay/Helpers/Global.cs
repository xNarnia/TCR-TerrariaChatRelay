using System.IO;

namespace TerrariaChatRelay
{
	public class Global
	{
		public static TCRConfig Config { get; set; }
		public static string SavePath { get; set; } = Path.Combine(Terraria.Main.SavePath, "TerrariaChatRelay");
		public static string ModConfigPath { get; set; } = Path.Combine(Terraria.Main.SavePath, "ModConfigs", "TerrariaChatRelay");
	}
}
