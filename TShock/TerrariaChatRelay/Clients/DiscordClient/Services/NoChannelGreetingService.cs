using Discord;
using Discord.WebSocket;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.DiscordClient.Helpers;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.Clients.DiscordClient.Services
{
	public class NoChannelGreetingService : IDiscordService
	{
		private DiscordSocketClient parentClient { get; set; }
		private IEnumerable<ulong> channel_ids { get; set; }
		public NoChannelGreetingService(DiscordSocketClient client, List<ulong> channel_ids) 
		{
			parentClient = client;
			this.channel_ids = channel_ids?.Where(x => x != 0) ?? new List<ulong>();
		}

		public void Start()
		{
			if (channel_ids.Count() == 0)
			{
				foreach (var guild in parentClient.Guilds)
				{
					// Find the first text channel where the bot has send message permissions.
					var writableChannel = guild.TextChannels
						.Where(channel => channel.GetPermissionOverwrite(guild.CurrentUser)?.SendMessages != PermValue.Deny)
						.Where(channel => channel.GetPermissionOverwrite(guild.CurrentUser)?.SendMessages != PermValue.Inherit ||
										  guild.EveryoneRole.Permissions.SendMessages)
						.FirstOrDefault();
					if (writableChannel != null)
					{
						Task.Run(async () =>
						{
							var embed = new EmbedBuilder()
								.WithTitle("This bot is powered by TerrariaChatRelay! 🌲")
								.WithDescription(
									@"To get started, begin by adding a channel using the `/addchannel` command!

									  If you don't see the `/addchannel` command, make sure your bot has the `Use Application Commands` permission.")
								.Build();
							try
							{
								await writableChannel.SendMessageAsync(null, false, embed);
							}
							catch (Exception e)
							{
								PrettyPrint.Log("Discord", "Error sending greeting message. Reason: " + e.Message);
							}
						});
						return;
					}
				}
			}
		}

		public void Stop()
		{

		}

		public void Dispose()
		{

		}
	}
}
