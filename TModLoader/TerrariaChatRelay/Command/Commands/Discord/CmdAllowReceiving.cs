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
	public class CmdAllowReceiving : ICommand
	{
		public string Name { get; } = "Allow Receiving Messages";

		public string CommandKey { get; } = "allowreceive";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Allow executing Discord channel to receive messages from the game.";

		public string Usage { get; } = "allowreceive";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			if (sender is not DiscordChatClient)
				return "Execution failed. Improper usage of method.";

			var endpoint = ((DiscordChatClient)sender).Endpoint;

			if (ulong.TryParse(input, out ulong channelId))
			{
				var targetEndpoint = DiscordPlugin.Config.EndPoints.Where(x => x.BotToken == endpoint.BotToken).First();

				if (targetEndpoint.DenyReceivingMessagesFromGame.Contains(channelId))
				{
					targetEndpoint.DenyReceivingMessagesFromGame.Remove(channelId);
					DiscordPlugin.Config.SaveJson();
					return "This channel will now receive messages from the game.";
				}
				else
				{
					return "This channel is already receiving messages from the game!";
				}
			}
			else
			{
				return "Fatal error removing channel ID.";
			}
		}
	}
}