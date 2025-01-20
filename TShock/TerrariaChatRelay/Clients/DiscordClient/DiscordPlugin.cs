using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TerrariaChatRelay.Clients;

namespace TerrariaChatRelay.Clients.DiscordClient
{
	public class DiscordPlugin : TCRPlugin
	{
		public static DiscordConfig Config { get; set; }

		public override void Init(List<IChatClient> Subscribers)
		{
			Config = new DiscordConfig().GetOrCreateConfiguration();

			if (Config.EnableDiscord)
			{
				foreach (var endpoint in Config.EndPoints)
					new DiscordChatClient(Subscribers, endpoint);

				if (Config.CommandPrefix.Length <= 0)
					Config.CommandPrefix = "t!";

				if (Config.SecondsToWaitBeforeRetryingAgain <= 0)
				{
					Config.SecondsToWaitBeforeRetryingAgain = 1;
					Config.SaveJson();
				}
			}
		}
	}
}