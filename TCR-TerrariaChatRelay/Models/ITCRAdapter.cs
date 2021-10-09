using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Models
{
	public interface ITCRAdapter
	{
		void BroadcastChatMessage(string msg, int excludedPlayerId);
		string ParseSnippets(string msg);
	}
}
