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
    public class SCRemoveManager : BaseSlashCommand
    {
        public override string Name => "RemoveManager";
        public override SlashCommandScope Scope => SlashCommandScope.Guild;
        public override string Description => "Removes the user from the managers group in TerrariaChatRelay.";
        public override bool Ephemeral => false;
        public override GuildPermission DefaultPermission => GuildPermission.Administrator;

        public override SlashCommandBuilder Builder(SlashCommandBuilder builder)
        {
            builder
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("user")
                    .WithDescription("User to remove from managers.")
                    .WithType(ApplicationCommandOptionType.User)
                    .WithRequired(true));

            return builder;
        }

        public override async Task Run(SocketSlashCommand command)
        {
            SocketUser user = command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value as SocketUser;
            if (DiscordPlugin.Config.ManagerUserIds.Contains(user.Id))
            {
                DiscordPlugin.Config.ManagerUserIds.RemoveAll(x => x ==  user.Id);
                DiscordPlugin.Config.SaveJson();

                await command.RespondAsync(null, [GetEmbed($"Manager successfully removed: " + user.Mention, Color.Green)]);
            }
            else
            {
                await command.RespondAsync(null, [GetEmbed($"User not found in manager group.", Color.Red)]);
            }
        }
    }
}
