using Terraria;

namespace TerrariaChatRelay.TMLHooks
{
    /// <summary>
    /// Event for catching players leaving.
    /// </summary>
    [TCRHook]
    public class HookPlayerLeave : BaseHook
    {
        public HookPlayerLeave(TerrariaChatRelay tcrMod) : base(tcrMod) { }
        public override void Attach() => On_RemoteClient.Reset += RemoteClient_Reset;
        public override void Detach() => On_RemoteClient.Reset -= RemoteClient_Reset;

        private void RemoteClient_Reset(On_RemoteClient.orig_Reset orig, RemoteClient self)
        {
            if (self.Id >= 0)
            {
                if (Main.player[self.Id].name != "")
                {
                    var tcrPlayer = Main.player[self.Id].ToTCRPlayer(-1);
                    Core.RaiseTerrariaMessageReceived(this, tcrPlayer, $"{tcrPlayer.Name} has left.");
                }
            }
            orig(self);
        }
    }
}
