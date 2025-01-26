using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
	public class SCReload : BaseSlashCommand
	{
		public override string Name => "Reload";
		public override SlashCommandScope Scope => SlashCommandScope.Guild;
		public override string Description => "Reloads TerrariaChatRelay.";
		public override bool Ephemeral => false;
		public override GuildPermission DefaultPermission => GuildPermission.Administrator;

		public override async Task Run(SocketSlashCommand command)
		{
			await command.RespondAsync(null, [GetEmbed("Reloading...", Color.Green)]);
			Core.DisconnectClients();
			Core.ConnectClients();
		}
	}
}
