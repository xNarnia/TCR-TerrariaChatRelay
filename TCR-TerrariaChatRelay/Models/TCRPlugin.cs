using System.Collections.Generic;
using TerrariaChatRelay.Clients;

namespace TerrariaChatRelay
{
	public abstract class TCRPlugin
	{
		public TCRPlugin()
		{
			Init(Core.Subscribers);
		}

		public abstract void Init(List<IChatClient> Subscribers);
	}
}
