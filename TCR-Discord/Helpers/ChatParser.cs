using TCRDiscord.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TCRDiscord.Helpers
{
    public class ChatParser
    {
        Regex specialFinder { get; }

        public ChatParser()
        {
            specialFinder = new Regex(@":[^:\s]*(?:::[^:\s]*)*>");
        }

        public string ConvertUserIdsToNames(string chatMessage, List<User> users)
        {
            foreach (var user in users)
            {
                chatMessage = chatMessage.Replace($"<@{user.Id}>", $"[c/00FFFF:@" + user.Username.Replace("[", "").Replace("]", "") + "]");
            }

            return chatMessage;
        }

        public string ShortenEmojisToName(string chatMessage)
        {
            chatMessage = specialFinder.Replace(chatMessage, ":");
            chatMessage = chatMessage.Replace("<:", ":");
            chatMessage = chatMessage.Replace("<a:", ":");

            return chatMessage;
        }
    }
}
