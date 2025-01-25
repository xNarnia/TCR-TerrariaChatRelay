using Discord.WebSocket;
using System;

namespace TerrariaChatRelay.Clients.DiscordClient.Services
{
	/// <summary>
	/// Performs independent logic and manages own responsibilities.
	/// </summary>
	public interface IDiscordService: IDisposable
	{
		void Start();
		void Stop();
	}
}
