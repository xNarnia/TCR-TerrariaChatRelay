using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
	public abstract class BaseSlashCommand : ISlashCommand
	{
		public abstract string Name { get; }
		public virtual string Description => "";
		public virtual bool Ephemeral { get; set; } = true;
		public virtual bool SilencePings { get; set; } = true;
		public abstract SlashCommandScope Scope { get; }
		public abstract GuildPermission DefaultPermission { get; }

		public virtual SlashCommandBuilder Builder(SlashCommandBuilder builder) { return builder; }
		public abstract Task Run(SocketSlashCommand command);

		public Embed GetEmbed(string text, Color? color = null)
		{
			var embedBuilder = new EmbedBuilder()
				.WithDescription(text);

			if (color != null)
				embedBuilder.WithColor(color.Value);

			return embedBuilder.Build();
		}
	}
}
