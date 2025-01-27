using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
	public class SCRemoveConsoleChannel : BaseSlashCommand
	{
		public override string Name => "RemoveConsoleChannel";
		public override SlashCommandScope Scope => SlashCommandScope.Guild;
		public override string Description => "Removes a console output relay channel from TerrariaChatRelay.";
		public override bool Ephemeral => false;
		public override GuildPermission DefaultPermission => GuildPermission.Administrator;

		public override SlashCommandBuilder Builder(SlashCommandBuilder builder)
		{
			builder
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("channel")
					.WithDescription("The channel you wish to remove.")
					.WithType(ApplicationCommandOptionType.Channel)
					.WithRequired(true))
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("endpoint")
					.WithDescription("The endpoint index you wish to add the relay chat to.")
					.WithType(ApplicationCommandOptionType.Integer));

			return builder;
		}

		public override async Task Run(SocketSlashCommand command)
		{
			SocketChannel channel = command.Data.Options.FirstOrDefault(o => o.Name == "channel")?.Value as SocketChannel;

			// The user supplies the index starting from 1 instead of 0
			int endpointIndexStartingFromOne = 0;
			var optionalParam = command.Data.Options.FirstOrDefault(o => o.Name == "endpoint");

			if (optionalParam != null)
				endpointIndexStartingFromOne = (int)(long)optionalParam.Value;

			var successMessage = $"<#{channel.Id}> successfully removed! Use /reload to load new changes.";
			var failMessage = "Console channel not found.";

			var embedBuilder = new EmbedBuilder();

			// Can't remove from what doesn't exist
			if (DiscordPlugin.Config.EndPoints.Count == 0)
			{
				// This shouldn't be possible, but just in case
				await command.RespondAsync(null, [GetEmbed("There are no endpoints to remove channels from.", Color.Red)]);
				return;
			}

			// User supplies endpoint
			if (endpointIndexStartingFromOne != 0)
			{
				if (DiscordPlugin.Config.EndPoints.Count < endpointIndexStartingFromOne)
				{
					await command.RespondAsync(null, [GetEmbed(endpointIndexStartingFromOne + $" is not a valid endpoint (there are only {DiscordPlugin.Config.EndPoints.Count} endpoints!).", Color.Red)]);
					return;
				}

				var endpoint = DiscordPlugin.Config.EndPoints[endpointIndexStartingFromOne - 1];
				if (endpoint.Console_Channel_IDs?.Contains(channel.Id) == true)
				{
					endpoint.Console_Channel_IDs.RemoveAll(x => x == channel.Id);
					DiscordPlugin.Config.SaveJson();
					await command.RespondAsync(null, [GetEmbed($"Endpoint {endpointIndexStartingFromOne}: {successMessage}", Color.Green)]);
				}
				else
				{
					await command.RespondAsync(null, [GetEmbed($"Endpoint {endpointIndexStartingFromOne}: {failMessage}", Color.Red)]);
				}
				return;
			}

			// User supplies channel but no endpoint, but has more than one endpoint
			if (DiscordPlugin.Config.EndPoints.Count >= 2)
			{
				await command.RespondAsync(null, [GetEmbed($"", Color.Green)]);
				string output = "";
				output += $"{DiscordPlugin.Config.EndPoints.Count} endpoints found!\n";

				var i = 1;
				foreach (var Endpoint in DiscordPlugin.Config.EndPoints)
				{
					Console.WriteLine($"[Endpoint {i}]");
					if (Endpoint.Console_Channel_IDs?.Count > 0)
					{
						foreach (var channelIdListing in Endpoint.Console_Channel_IDs)
						{
							output += $"\n- Channel: {channelIdListing}";
						}
					}
					else
					{
						output += "\n- No channels found";
					}
					i++;
				}

				output += "\nTo update the channel ids, add the number of the endpoint you wish to update.";
				output += "- Example: /removeconsolechannel 1 CHANNELIDHERE";
				await command.RespondAsync(null, [GetEmbed(output)]);
				return;
			}

			// User supplies only channel
			if (DiscordPlugin.Config.EndPoints.Count == 1)
			{
				var endpoint = DiscordPlugin.Config.EndPoints.First();
				if (endpoint.Console_Channel_IDs.Contains(channel.Id))
				{
					endpoint.Console_Channel_IDs.RemoveAll(x => x == channel.Id);
					DiscordPlugin.Config.SaveJson();
					await command.RespondAsync(null, [GetEmbed(successMessage, Color.Green)]);
				}
				else
				{
					await command.RespondAsync(null, [GetEmbed(failMessage, Color.Red)]);
				}
				return;
			}

			await command.RespondAsync(null, [GetEmbed("An unexpected error occurred using /removeconsolechannel.", Color.Red)]);
			return;
		}
	}
}
