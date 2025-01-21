using System.Linq;
using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay.Command.Commands.Discord
{
	[Command]
	public class CmdDenyReceiving : ICommand
	{
		public string Name { get; } = "Deny Receiving Messages";

		public string CommandKey { get; } = "denyreceive";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Deny executing Discord channel from receiving messages from the game. Command execution and responses not prevented.";

		public string Usage { get; } = "denyreceive";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			if (sender is not DiscordChatClient)
				return "Execution failed. Improper usage of method.";

			var endpoint = ((DiscordChatClient)sender).Endpoint;

			if (ulong.TryParse(input, out ulong channelId))
			{
				var targetEndpoint = DiscordPlugin.Config.EndPoints.Where(x => x.BotToken == endpoint.BotToken).First();

				if (!targetEndpoint.DenyReceivingMessagesFromGame.Contains(channelId))
				{
					targetEndpoint.DenyReceivingMessagesFromGame.Add(channelId);
					DiscordPlugin.Config.SaveJson();
					return "This channel will no longer receive messages from the game.";
				}
				else
				{
					return "This channel is already denying messages from the game!";
				}
			}
			else
			{
				return "Fatal error removing channel ID.";
			}
		}
	}
}