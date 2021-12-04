using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRDiscord.Models
{
    public class MessageData
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("channel_id")]
        public ulong ChannelId { get; set; }

        [JsonProperty("guild_id")]
        public ulong GuildId { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("author")]
        public User Author { get; set; }

        [JsonProperty("content")]
        public string Message { get; set; }

        [JsonProperty("mentions")]
        public List<User> UsersMentioned { get; set; }

        [JsonProperty("mention_roles")]
        public List<ulong> RolesMentoned { get; set; }

        [JsonProperty("mention_everyone")]
        public bool IsMentioningEveryone { get; set; }

        [JsonProperty("tts")]
        public bool IsTextToSpeech { get; set; }

        [JsonProperty("pinned")]
        public bool IsPinned { get; set; }
    }
}