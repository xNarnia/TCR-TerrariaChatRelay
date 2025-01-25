using Discord.Net;
using Discord.WebSocket;
using System.Net.Sockets;
using System.Threading.Tasks;
using System;
using System.Timers;
using TerrariaChatRelay.Helpers;
using System.Collections.Generic;
using TerrariaChatRelay.Clients.DiscordClient.Helpers;
using System.Linq;

namespace TerrariaChatRelay.Clients.DiscordClient.Services
{
	public class GameStatusService : IDiscordService
	{
		private Timer gameStatusTimer { get; set; }
		private DiscordSocketClient socket { get; set; }
		private ChatParser chatParser { get; set; }

		public GameStatusService(DiscordSocketClient client, Timer timer, ChatParser parser)
		{
			this.socket = client;
			chatParser = parser;

			if (DiscordPlugin.Config.BotGameStatus != null && DiscordPlugin.Config.BotGameStatus != "")
			{
				gameStatusTimer = timer;
				gameStatusTimer.Elapsed += UpdateGameStatus;
			}
		}

		public void Start() => gameStatusTimer.Start();
		public void Stop() => gameStatusTimer.Stop();
		public void Dispose()
		{
			gameStatusTimer.Stop();
			gameStatusTimer.Dispose();
		}

		/// <summary>
		/// Continuously updates the Game Status of the bot.
		/// </summary>
		private async void UpdateGameStatus(object sender, ElapsedEventArgs e)
		{
			var status = DiscordPlugin.Config.BotGameStatus;
			status = chatParser.ReplaceCustomStringVariables(status);

			try
			{
				// Don't send an update if the status hasn't changed
				if (socket.CurrentUser.Activities?.FirstOrDefault()?.Name != status)
					await socket.SetGameAsync(status);
			}
			catch (Exception ex)
			{
				PrettyPrint.Log("Discord", "Unable to set game status.");
			}
		}
	}
}
