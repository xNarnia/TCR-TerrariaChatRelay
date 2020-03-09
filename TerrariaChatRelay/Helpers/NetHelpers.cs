using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.Net;

namespace TerrariaChatRelay.Helpers
{
    public static class NetHelpers
    {
        public static void BroadcastChatMessageWithoutTCRFormattable(string text, int excludedPlayer)
        {
            NetPacket packet = 
				Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(
					NetworkText.FromFormattable(text), new Color(255, 255, 255), byte.MaxValue);
            NetManager.Instance.Broadcast(packet, excludedPlayer);
        }


		public static void BroadcastChatMessageWithoutTCRLiteral(string text, int excludedPlayer)
		{
			NetPacket packet = 
				Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(
					NetworkText.FromLiteral(text), new Color(255, 255, 255), byte.MaxValue);
			NetManager.Instance.Broadcast(packet, excludedPlayer);
		}
	}
}
