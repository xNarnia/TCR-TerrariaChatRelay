using Terraria.Chat;
using Terraria;
using System.IO;
using Terraria.GameContent.NetModules;

namespace TerrariaChatRelay.TMLHooks
{
    /// <summary>
    /// Event for catching in-game player chat messages.
    /// </summary>
    [TCRHook]
    public class HookPlayerChat : BaseHook
    {
        public HookPlayerChat(TerrariaChatRelay tcrMod) : base(tcrMod) { }
        public override void Attach() => On_ChatCommandProcessor.ProcessIncomingMessage += On_ChatCommandProcessor_ProcessIncomingMessage;
        public override void Detach() => On_ChatCommandProcessor.ProcessIncomingMessage -= On_ChatCommandProcessor_ProcessIncomingMessage;

        private void On_ChatCommandProcessor_ProcessIncomingMessage(On_ChatCommandProcessor.orig_ProcessIncomingMessage orig, ChatCommandProcessor self, ChatMessage message, int clientId)
        {
            // If SubworldLib is present, remove the hook from Subworlds
            // This prevents double posting, allowing the main server to relay for both worlds
            if (TCRMod.SubworldLib != null)
            {
                object current = TCRMod.SubworldLib.Call("Current");
                if (current?.ToString().ToLower() != "false")
                {
                    On_ChatCommandProcessor.ProcessIncomingMessage -= On_ChatCommandProcessor_ProcessIncomingMessage;
                    orig(self, message, clientId);
                    return;
                }
            }

            // Not relaying commands with / as those are typically for commands with sensitive information
            if (Global.Config.ShowChatMessages && !message.Text.StartsWith("/"))
            {
                Core.RaiseTerrariaMessageReceived(this, new TCRPlayer()
                {
                    PlayerId = clientId,
                    Name = Main.player[clientId].name
                }, message.Text);
            }
            orig(self, message, clientId);
        }

        /// <summary>
        /// Event to intercept chat messages sent from players.
        /// - No longer in use
        /// </summary>
        private bool NetTextModule_DeserializeAsServer(On_NetTextModule.orig_DeserializeAsServer orig, Terraria.GameContent.NetModules.NetTextModule self, BinaryReader reader, int senderPlayerId)
        {
            long savedPosition = reader.BaseStream.Position;
            ChatMessage message = ChatMessage.Deserialize(reader);

            if (Global.Config.ShowChatMessages)
                Core.RaiseTerrariaMessageReceived(this, new TCRPlayer()
                {
                    PlayerId = senderPlayerId,
                    Name = Main.player[senderPlayerId].name
                }, message.Text);

            reader.BaseStream.Position = savedPosition;
            return orig(self, reader, senderPlayerId);
        }
    }
}
