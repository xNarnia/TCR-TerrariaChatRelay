using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
	public interface ISlashCommand
	{
		/// <summary>
		/// Name of the command. Must match the regular expression ^[\w-]{3,32}$
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Describes the command.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Whether the command is only visible to the calling user.
		/// </summary>
		bool Ephemeral { get; set; }

		/// <summary>
		/// Whether to silence pings to users/roles.
		/// </summary>
		bool SilencePings { get; set; }

		/// <summary>
		/// The default permission assigned to the command when registered.
		/// </summary>
		GuildPermission DefaultPermission { get; }

		/// <summary>
		/// The scope of the command.
		/// </summary>
		SlashCommandScope Scope { get; }

		/// <summary>
		/// Slash command property and option assignments.
		/// <para>The name, description, and default permission are automatically set.</para>
		/// </summary>
		/// <param name="builder">The builder used to build this command.</param>
		/// <returns>The builder used to build this command.</returns>
		SlashCommandBuilder Builder(SlashCommandBuilder builder);

		/// <summary>
		/// The action to execute for this command.
		/// </summary>
		/// <param name="command">The command handler sent from Discord.</param>
		/// <returns>The response from the command.</returns>
		Task Run(SocketSlashCommand command);
	}
}
