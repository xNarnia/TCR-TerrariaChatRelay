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
	public class SCViewManagers : BaseSlashCommand
	{
		public override string Name => "Managers";
		public override SlashCommandScope Scope => SlashCommandScope.Guild;
		public override string Description => "Shows all TCR managers.";
		public override bool Ephemeral => false;
		public override GuildPermission DefaultPermission => GuildPermission.SendMessages;

		public override async Task Run(SocketSlashCommand command)
		{
			var managerIds = DiscordPlugin.Config.ManagerUserIds.Where(x => x != 0);
			string managerString = "";

			if (managerIds.Count() > 0)
				managerString = string.Join("\n", managerIds.Select(x => $"<@{x}>"));
			else
				managerString = "No users found.";

			var embed = new EmbedBuilder()
				.WithTitle("Managers")
				.WithDescription(managerString)
				.Build();

			await command.RespondAsync(null, [embed], false, false, AllowedMentions.None);
		}
	}
}
