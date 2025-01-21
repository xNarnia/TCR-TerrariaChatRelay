using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.Net;
using Terraria.UI.Chat;
using TerrariaChatRelay.Models;

namespace TerrariaChatRelay
{
	public class Adapter : ITCRAdapter
	{
		public void BroadcastChatMessage(string msg, int excludedPlayerId)
		{
			if (msg == null)
				return;

			NetPacket packet =
				Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(
					NetworkText.FromFormattable(msg), new Color(255, 255, 255), byte.MaxValue);
			NetManager.Instance.Broadcast(packet, excludedPlayerId);
		}

		public string ParseSnippets(string msg)
		{
			var snippets = ChatManager.ParseMessage(msg, Color.White);

			string outmsg = "";
			foreach (var snippet in snippets)
			{
				outmsg += snippet.Text;
			}
			return outmsg;
		}
	}
}
