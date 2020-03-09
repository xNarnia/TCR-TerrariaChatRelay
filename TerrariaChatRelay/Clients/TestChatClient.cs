using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay.Clients
{
    public class TestChatClient : BaseClient
    {
        public TestChatClient(List<IChatClient> parent) : base(parent) { }

        public override void Connect()
        {

        }

        public override void Disconnect()
        {

        }

        public override void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs e)
        {
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(e.Message + " - TestChatClient"), Color.Cyan, -1);
        }

        public override void GameMessageSentHandler(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
