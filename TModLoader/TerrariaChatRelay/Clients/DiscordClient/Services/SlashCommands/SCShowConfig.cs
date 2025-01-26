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
	public class SCShowConfig : BaseSlashCommand
	{
		public override string Name => "TCRConfig";
		public override SlashCommandScope Scope => SlashCommandScope.Guild;
		public override string Description => "Shows the currently loaded server config only to you.";
		public override bool Ephemeral => true;
		public override GuildPermission DefaultPermission => GuildPermission.Administrator;

		public override async Task Run(SocketSlashCommand command)
		{
			var template = "```json\n{0}\n```";
			var rawConfig = DiscordPlugin.Config.ToJson();
			var lines = rawConfig.Split(Environment.NewLine.ToCharArray())
				.Where(x => !x.Contains("\"Help") && x != "")
				.ToArray();

			rawConfig = string.Join(Environment.NewLine, lines);
			rawConfig = Regex.Replace(rawConfig, @"""BotToken"":\s*""([^""]+)""", "BotToken REDACTED");

			if (template.Length + rawConfig.Length > 2000)
			{
				rawConfig = rawConfig.Substring(0, 1999 - template.Length);
			}

			await command.RespondAsync(string.Format(template, rawConfig), null, false, true);
		}
	}
}
