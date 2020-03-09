using System.Collections.Generic;
using TerrariaChatRelay;
using TerrariaChatRelay.Clients.Interfaces;

namespace DiscordChatRelay
{
	public class Main : TCRPlugin
	{
		public static Configuration Config { get; set; }

		public override void Init(List<IChatClient> Subscribers)
		{
			Config = (Configuration)new Configuration().GetOrCreateConfiguration();

			if (Config.EnableDiscord)
			{
				foreach (var discordClient in Config.EndPoints)
					new ChatClient(Subscribers, discordClient.BotToken, discordClient.Channel_IDs);
			}
		}
	}
}
