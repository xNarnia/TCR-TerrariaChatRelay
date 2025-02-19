using Terraria;

namespace TerrariaChatRelay.TMLHooks
{
    /// <summary>
    /// Event for catching players entering after they have loaded in.
    /// </summary>
    [TCRHook]
    public class HookPlayerJoin : BaseHook
    {
        public HookPlayerJoin(TerrariaChatRelay tcrMod) : base(tcrMod) { }
        public override void Attach() => On_NetMessage.SyncConnectedPlayer += OnPlayerJoin_NetMessage_SyncConnectedPlayer;
        public override void Detach() => On_NetMessage.SyncConnectedPlayer -= OnPlayerJoin_NetMessage_SyncConnectedPlayer;

        private void OnPlayerJoin_NetMessage_SyncConnectedPlayer(On_NetMessage.orig_SyncConnectedPlayer orig, int plr)
        {
            orig(plr);
            var tcrPlayer = Main.player[plr].ToTCRPlayer(-1);
            Core.RaiseTerrariaMessageReceived(this, tcrPlayer, $"{tcrPlayer.Name} has joined.", TerrariaChatSource.PlayerEnter);
        }
    }
}
