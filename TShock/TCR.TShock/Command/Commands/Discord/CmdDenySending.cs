using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.DiscordClient;
using TerrariaChatRelay.Command;

namespace TerrariaChatRelay.Command.Commands.Discord
{
	[Command]
	public class CmdDenySending : ICommand
	{
		public string Name { get; } = "Deny Sending Messages";

		public string CommandKey { get; } = "denysend";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Deny executing Discord channel from sending messages to the game. Command execution and responses not prevented.";

		public string Usage { get; } = "denysend";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			if (sender is not DiscordChatClient)
				return "Execution failed. Improper usage of method.";

			var endpoint = ((DiscordChatClient)sender).Endpoint;

			if (ulong.TryParse(input, out ulong channelId))
			{
				var targetEndpoint = DiscordPlugin.Config.EndPoints.Where(x => x.BotToken == endpoint.BotToken).First();

				if (!targetEndpoint.DenySendingMessagesToGame.Contains(channelId))
				{
					targetEndpoint.DenySendingMessagesToGame.Add(channelId);
					DiscordPlugin.Config.SaveJson();
					return "This channel will no longer send messages to the game.";
				}
				else
				{
					return "This channel is already denying messages to the game!";
				}
			}
			else
			{
				return "Fatal error adding channel ID.";
			}
		}
	}
}