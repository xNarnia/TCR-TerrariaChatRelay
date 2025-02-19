using System;
using System.IO;
using Terraria.IO;
using Terraria;
using TerrariaChatRelay.Helpers;
using Terraria.Localization;

namespace TerrariaChatRelay.TMLHooks
{
    /// <summary>
    /// Event to send a message when the server is loading.
    /// </summary>
    [TCRHook]
    public class HookServerLoading : BaseHook
    {
        public HookServerLoading(TerrariaChatRelay tcrMod) : base(tcrMod) { }
        public override void Attach() => On_WorldFile.LoadWorld_Version2 += OnWorldLoadStart;
        public override void Detach() => On_WorldFile.LoadWorld_Version2 -= OnWorldLoadStart;

        private int OnWorldLoadStart(On_WorldFile.orig_LoadWorld_Version2 orig, BinaryReader reader)
        {
            try
            {
                if (!Netplay.Disconnect)
                {
                    if (Global.Config.ShowServerStartMessage)
                        Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, Language.GetTextValue("LegacyMenu.8"), TerrariaChatSource.ServerStart);

                    if (TCRMod.LatestVersion > TCRMod.Version)
                        Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, $"A new version of TCR is available: V.{TCRMod.LatestVersion.ToString()}", TerrariaChatSource.TCR);
                }
            }
            catch (Exception e)
            {
                PrettyPrint.Log("Adapter", "Error checking for version update: " + e.Message, ConsoleColor.Red);
            }

            return orig(reader);
        }
    }
}
