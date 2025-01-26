using Discord.Net;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using System.Timers;
using TerrariaChatRelay.Helpers;
using System.Collections.Generic;
using TerrariaChatRelay.Clients.DiscordClient.Helpers;

namespace TerrariaChatRelay.Clients.DiscordClient.Services
{
	public class ChannelDescriptionService : IDiscordService
	{
		private Timer channelDescriptionTimer { get; set; }
		private DiscordSocketClient socket { get; set; }
		private List<ulong> channel_ids { get; set; }
		private ChatParser chatParser { get; set; }

		public ChannelDescriptionService(DiscordSocketClient client, List<ulong> channels, Timer timer, ChatParser parser)
		{
			if (DiscordPlugin.Config.BotChannelDescription != null && DiscordPlugin.Config.BotChannelDescription != "")
			{
				channelDescriptionTimer = timer;
				channelDescriptionTimer.Elapsed += UpdateChannelDescription;
			}

			this.socket = client;
			channel_ids = channels;
			chatParser = parser;
		}

		public void Start() => channelDescriptionTimer?.Start();
		public void Stop() => channelDescriptionTimer?.Stop();
		public void Dispose()
		{
			channelDescriptionTimer?.Stop();
			channelDescriptionTimer?.Dispose();
		}

		/// <summary>
		/// Continuously updates the Channel Descriptions the bot is relaying in.
		/// </summary>
		private async void UpdateChannelDescription(object sender, ElapsedEventArgs e)
		{
			foreach (var channelId in channel_ids)
			{
				var channel = socket.GetChannel(channelId);
				if (channel is SocketTextChannel)
				{
					var textChannel = (SocketTextChannel)channel;
					var topic = DiscordPlugin.Config.BotChannelDescription;
					topic = chatParser.ReplaceCustomStringVariables(topic);

					if (topic == textChannel.Topic)
						return;

					try
					{
						await textChannel.ModifyAsync(x => x.Topic = topic);
					}
					catch (Exception ex)
					{
						if (ex.Message.ToLower().Contains("missing permission"))
						{
							PrettyPrint.Log("Discord", "Missing Permission - Manage Channels: Could not update channel description.", ConsoleColor.Red);
							PrettyPrint.Log("Discord", $"   Update permissions then restart using {DiscordPlugin.Config.CommandPrefix}restart.", ConsoleColor.Red);
							channelDescriptionTimer.Stop();
						}
						else
						{
							channelDescriptionTimer.Stop();
							PrettyPrint.Log("Discord", "Unable to set channel description. Reason: " + ex.Message);
						}
					}
				}
				await Task.Delay(50);
			}
		}
	}
}
