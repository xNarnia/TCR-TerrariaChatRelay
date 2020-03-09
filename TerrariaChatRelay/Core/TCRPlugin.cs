using System.Collections.Generic;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay
{
	public abstract class TCRPlugin
	{
		public TCRPlugin()
		{
			Init(EventManager.Subscribers);
		}

		public abstract void Init(List<IChatClient> Subscribers);
	}
}
