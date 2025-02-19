using Terraria;
using Terraria.Localization;

namespace TerrariaChatRelay.TMLHooks
{
    /// <summary>
    /// Event for detecting server stopping.
    /// </summary>
    [TCRHook]
    public class HookServerStop : BaseHook
    {
        public HookServerStop(TerrariaChatRelay tcrMod) : base(tcrMod) { }
        public override void Attach() => On_Netplay.StopListening += OnServerStop;
        public override void Detach() => On_Netplay.StopListening -= OnServerStop;

        private void OnServerStop(On_Netplay.orig_StopListening orig)
        {
            if (Global.Config.ShowServerStopMessage)
                Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, Language.GetTextValue("Net.ServerSavingOnExit"), TerrariaChatSource.ServerStop);

            orig();
        }
    }
}
