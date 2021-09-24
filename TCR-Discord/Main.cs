using System.Collections.Generic;
using TerrariaChatRelay;
using TerrariaChatRelay.Clients;

namespace TCRDiscord
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

				if(Config.CommandPrefix.Length < 1)
					Config.CommandPrefix = "!";
			}

			// not appropriate to have a ScanForCommands method in the interface but too lazy to think this out
			((CommandService)Core.CommandServ).ScanForCommands(this);
		}
	}
}
