namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
	/// <summary>
	/// The context in which a command will run.
	/// </summary>
	public enum SlashCommandScope
	{
		/// <summary>
		/// Command runs in a Discord server.
		/// </summary>
		Guild,
		/// <summary>
		/// Command can be run anywhere, including outside of a Discord server.
		/// </summary>
		Global
	}
}
