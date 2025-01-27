using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria;
using System.IO;
using System.Reflection;
using Terraria.IO;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands
{
    // > Broken for some reason

    //public class SCServerLog : BaseSlashCommand
    //{
    //    public override string Name => "ServerLog";
    //    public override SlashCommandScope Scope => SlashCommandScope.Guild;
    //    public override string Description => "Sends the latest server.log as an attachment silently to you.";
    //    public override bool Ephemeral => true;
    //    public override GuildPermission DefaultPermission => GuildPermission.Administrator;

    //    public override async Task Run(SocketSlashCommand command)
    //    {
    //        var path = Path.Combine(Directory.GetCurrentDirectory(), "tModLoader-Logs", "server.log");
    //        if (File.Exists(path))
    //        {
    //            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    //            {
    //                var fileAttachment = new FileAttachment(fileStream, "server.log");

    //                // Send a follow-up response with the file
    //                await command.RespondWithFileAsync(fileAttachment);
    //            }
    //        }
    //        else
    //            await command.RespondAsync(null, [GetEmbed("`server.log` not found.")]);
    //    }
    //}
}
