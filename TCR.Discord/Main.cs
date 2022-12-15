using System.Collections.Generic;
using TCRCore;
using TCRCore.Clients;

namespace TCRDiscord
{
	public class Main : TCRPlugin
	{
		public static Configuration Config { get; set; }

		public override void Init(List<IChatClient> Subscribers)
		{
			Config = new Configuration().GetOrCreateConfiguration();

			if (Config.EnableDiscord)
			{
				foreach (var endpoint in Config.EndPoints)
					new ChatClient(Subscribers, endpoint);

				if (Config.CommandPrefix.Length <= 0)
					Config.CommandPrefix = "t!";

				if (Config.SecondsToWaitBeforeRetryingAgain <= 0)
				{
					Config.SecondsToWaitBeforeRetryingAgain = 1;
					Config.SaveJson();
				}
			}

			// not appropriate to have a ScanForCommands method in the interface but too lazy to think this out
			((CommandService)Core.CommandServ).ScanForCommands(this);
		}
	}
}