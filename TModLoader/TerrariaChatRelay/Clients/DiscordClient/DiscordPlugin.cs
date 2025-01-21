using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

			// If config is missing entries or not indented, fix it
			var rawConfig = File.ReadAllText(Config.FileName);
			var toJsonConfig = Config.ToJson();
			if (rawConfig != toJsonConfig)
			{
				File.WriteAllText(Config.FileName, toJsonConfig);
			}

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