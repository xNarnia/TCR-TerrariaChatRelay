using System;
using System.Linq;
using Terraria.Chat;
using Terraria.Localization;
using Terraria;
using Microsoft.Xna.Framework;

namespace TerrariaChatRelay.TMLHooks
{
    /// <summary>
    /// Event to intercept all other messages from Terraria. E.g. blood moon, death notifications, and player join/leaves.
    /// </summary>
    [TCRHook]
    public class HookWorldEvent : BaseHook
    {
        public string PlayerJoinEndingString;
        public string PlayerLeaveEndingString;

        public HookWorldEvent(TerrariaChatRelay tcrMod) : base(tcrMod)
        {
            PlayerJoinEndingString = Language.GetText("LegacyMultiplayer.19").Value.Split(new string[] { "{0}" }, StringSplitOptions.None).Last();
            PlayerLeaveEndingString = Language.GetText("LegacyMultiplayer.20").Value.Split(new string[] { "{0}" }, StringSplitOptions.None).Last();
        }

        public override void Attach() => On_ChatHelper.BroadcastChatMessage += BroadcastChatMessage;
        public override void Detach() => On_ChatHelper.BroadcastChatMessage -= BroadcastChatMessage;

        private void BroadcastChatMessage(On_ChatHelper.orig_BroadcastChatMessage orig, NetworkText text, Color color, int excludedPlayer)
        {
            if (Global.Config.ShowGameEvents && !text.ToString().EndsWith(PlayerJoinEndingString) && !text.ToString().EndsWith(PlayerLeaveEndingString))
                Core.RaiseTerrariaMessageReceived(this, (excludedPlayer > 0 ? Main.player[excludedPlayer].ToTCRPlayer(excludedPlayer) : TCRPlayer.Server), text.ToString());

            orig(text, color, excludedPlayer);
        }
    }
}
