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
	public class SCViewAdmins : BaseSlashCommand
	{
		public override string Name => "Admins";
		public override SlashCommandScope Scope => SlashCommandScope.Guild;
		public override string Description => "Shows all TCR administrators.";
		public override bool Ephemeral => false;
		public override GuildPermission DefaultPermission => GuildPermission.SendMessages;

		public override async Task Run(SocketSlashCommand command)
		{
			var adminIds = DiscordPlugin.Config.AdminUserIds.Where(x => x != 0);
			string adminString = "";

			if (adminIds.Count() > 0)
				adminString = string.Join("\n", adminIds.Select(x => $"<@{x}>"));
			else
				adminString = "No users found.";

			var embed = new EmbedBuilder()
				.WithTitle("Administrators")
				.WithDescription(adminString)
				.Build();

			await command.RespondAsync(null, [embed], false, false, AllowedMentions.None);
		}
	}
}
