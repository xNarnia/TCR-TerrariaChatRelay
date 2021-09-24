using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay
{
    public class TCRConfig : SimpleConfig<TCRConfig>
    {
        public override string FileName { get; set; }
            = Path.Combine(Global.ModConfigPath, "TCR.json");

        // TerrariaChatRelay
        public bool ShowChatMessages { get; set; } = true;
        public bool ShowGameEvents { get; set; } = true;
		public bool ShowServerStartMessage { get; set; } = true;
		public bool ShowServerStopMessage { get; set; } = true;
		public bool CheckForLatestVersion { get; set; } = true;

        public TCRConfig()
        {
            if (!File.Exists(FileName))
            {
                // Discord
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("TerrariaChatRelay - Mod Config Generated: " + FileName);
                Console.ResetColor();
            }
        }
    }
}