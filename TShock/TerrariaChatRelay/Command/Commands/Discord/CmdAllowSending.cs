using System.Linq;
using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay.Command.Commands.Discord
{
	[Command]
	public class CmdAllowSending : ICommand
	{
		public string Name { get; } = "Allow Sending Messages";

		public string CommandKey { get; } = "allowsend";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Allow executing Discord channel to send messages to the game.";

		public string Usage { get; } = "allowsend";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			if (sender is not DiscordChatClient)
				return "Execution failed. Improper usage of method.";

			var endpoint = ((DiscordChatClient)sender).Endpoint;

			if (ulong.TryParse(input, out ulong channelId))
			{
				var targetEndpoint = DiscordPlugin.Config.EndPoints.Where(x => x.BotToken == endpoint.BotToken).First();

				if (targetEndpoint.DenySendingMessagesToGame.Contains(channelId))
				{
					targetEndpoint.DenySendingMessagesToGame.Remove(channelId);
					DiscordPlugin.Config.SaveJson();
					return "This channel will now send messages to the game.";
				}
				else
				{
					return "This channel is already sending messages to the game!";
				}
			}
			else
			{
				return "Fatal error removing channel ID.";
			}
		}
	}
}