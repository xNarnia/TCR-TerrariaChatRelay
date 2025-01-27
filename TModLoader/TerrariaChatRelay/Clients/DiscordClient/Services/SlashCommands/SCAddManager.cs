using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
    public class SCAddManager : BaseSlashCommand
    {
        public override string Name => "AddManager";
        public override SlashCommandScope Scope => SlashCommandScope.Guild;
        public override string Description => "Adds the user as a manager to TerrariaChatRelay.";
        public override bool Ephemeral => false;
        public override GuildPermission DefaultPermission => GuildPermission.Administrator;

        public override SlashCommandBuilder Builder(SlashCommandBuilder builder)
        {
            builder
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("user")
                    .WithDescription("User to add to managers.")
                    .WithType(ApplicationCommandOptionType.User)
                    .WithRequired(true));

            return builder;
        }

        public override async Task Run(SocketSlashCommand command)
        {
            SocketUser user = command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value as SocketUser;
            DiscordPlugin.Config.ManagerUserIds.Add(user.Id);
            DiscordPlugin.Config.SaveJson();

            await command.RespondAsync(null, [GetEmbed($"Manager successfully added: " + user.Mention, Color.Green)]);
        }
    }
}
