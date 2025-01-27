using Terraria.GameContent.NetModules;
using Terraria.Localization;
using Terraria.Net;
using Terraria;
using Microsoft.Xna.Framework;

namespace TerrariaChatRelay.TMLHooks
{
    /// <summary>
    /// Event for catching players entering before they are loaded in.
    /// </summary>
    [TCRHook]
    public class HookPlayerLoading : BaseHook
    {
        public HookPlayerLoading(TerrariaChatRelay tcrMod) : base(tcrMod) { }
        public override void Attach() => On_NetMessage.greetPlayer += NetMessage_greetPlayer;
        public override void Detach() => On_NetMessage.greetPlayer -= NetMessage_greetPlayer;

        private void NetMessage_greetPlayer(On_NetMessage.orig_greetPlayer orig, int plr)
        {
            NetPacket packet = NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("This chat is powered by TerrariaChatRelay"), Color.LawnGreen, byte.MaxValue);
            NetManager.Instance.SendToClient(packet, plr);
            orig(plr);
        }
    }
}
