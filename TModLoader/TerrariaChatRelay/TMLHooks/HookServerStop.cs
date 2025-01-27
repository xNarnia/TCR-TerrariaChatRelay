using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Microsoft.Xna.Framework;

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
                Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, "The server is stopping!");

            orig();
        }
    }
}
