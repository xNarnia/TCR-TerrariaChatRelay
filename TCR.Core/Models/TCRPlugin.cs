using System.Collections.Generic;
using TCRCore.Clients;

namespace TCRCore
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
