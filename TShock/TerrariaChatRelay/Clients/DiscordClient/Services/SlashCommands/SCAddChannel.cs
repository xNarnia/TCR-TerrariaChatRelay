using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
	public class SCAddChannel : BaseSlashCommand
	{
		public override string Name => "addchannel";
		public override SlashCommandScope Scope => SlashCommandScope.Guild;
		public override string Description => "Adds a relay channel to TerrariaChatRelay.";
		public override bool Ephemeral => false;
		public override GuildPermission DefaultPermission => GuildPermission.Administrator;

		public override SlashCommandBuilder Builder(SlashCommandBuilder builder)
		{
			builder
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("channel")
					.WithDescription("The channel you wish to relay chat to.")
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
			SocketGuildChannel channel = command.Data.Options.FirstOrDefault(o => o.Name == "channel")?.Value as SocketGuildChannel;

            // The user supplies the index starting from 1 instead of 0
            int endpointIndexStartingFromOne = 0;
			var optionalParam = command.Data.Options.FirstOrDefault(o => o.Name == "endpoint");

			if (optionalParam != null)
				endpointIndexStartingFromOne = (int)(long)optionalParam.Value;

			var successMessage = $"<#{channel.Id}> successfully added! Use /reload to load new changes.";

			var embedBuilder = new EmbedBuilder();

			// User supplies endpoint
			if (endpointIndexStartingFromOne != 0)
			{
				if (DiscordPlugin.Config.EndPoints.Count < endpointIndexStartingFromOne)
				{
					await command.RespondAsync(null, [GetEmbed($"{endpointIndexStartingFromOne} is not a valid endpoint (there are only {DiscordPlugin.Config.EndPoints.Count} endpoints!).", Color.Red)]);
					return;
				}

				var endpoint = DiscordPlugin.Config.EndPoints[endpointIndexStartingFromOne - 1];

				if (endpoint.Channel_IDs.Contains(channel.Id))
				{
                    await command.RespondAsync(null, [GetEmbed($"<#{channel.Id}> is already relaying chat!", Color.Red)]);
					return;
                }

                endpoint.Channel_IDs.RemoveAll(x => x == 0);
				endpoint.Channel_IDs.Add(channel.Id);
				DiscordPlugin.Config.SaveJson();

				await command.RespondAsync(null, [GetEmbed($"Endpoint {endpointIndexStartingFromOne}: {successMessage}", Color.Green)]);
				return;
			}

			// User supplies channel but no endpoint, but has more than one endpoint
			if (DiscordPlugin.Config.EndPoints.Count >= 2)
			{
				string output = "";
				output += $"{DiscordPlugin.Config.EndPoints.Count} endpoints found!\n";

				var i = 1;
				foreach (var Endpoint in DiscordPlugin.Config.EndPoints)
				{
					output += $"\n[Endpoint {i}]";
					if (Endpoint.Channel_IDs?.Count > 0)
					{
						foreach (var channelIdListing in Endpoint.Channel_IDs)
						{
							output += $"\n- Channel: <#{channelIdListing}>";
						}
					}
					else
					{
						output += $"\n- No channels found";
					}

					i++;
				}

				await command.RespondAsync(null, [GetEmbed(
					$"\n\nTo update the channel ids, add the number of the endpoint you wish to update." +
					$"\n- Example: /addchannel channel:#my-channel endpoint:1 ")]);
				return;
			}

			// User supplies only channel
			if (DiscordPlugin.Config.EndPoints.Count == 0)
			{
				DiscordPlugin.Config.EndPoints.Add(new Endpoint()
				{
					Channel_IDs = { channel.Id }
				});
				DiscordPlugin.Config.SaveJson();
				await command.RespondAsync(null, [GetEmbed(successMessage, Color.Green)]);
				return;
			}
			if (DiscordPlugin.Config.EndPoints.Count == 1)
			{
				var endpoint = DiscordPlugin.Config.EndPoints.First();

                if (endpoint.Channel_IDs.Contains(channel.Id))
                {
                    await command.RespondAsync(null, [GetEmbed($"<#{channel.Id}> is already relaying chat!", Color.Red)]);
                    return;
                }

                endpoint.Channel_IDs.RemoveAll(x => x == 0);
				endpoint.Channel_IDs.Add(channel.Id);
				DiscordPlugin.Config.SaveJson();
				await command.RespondAsync(null, [GetEmbed(successMessage, Color.Green)]);
				return;
			}

			await command.RespondAsync(null, [GetEmbed("An unexpected error occurred using /addchannel.", Color.Red)]);
		}
	}
}
