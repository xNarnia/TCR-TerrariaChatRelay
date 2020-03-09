using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordChatRelay.Models;

namespace DiscordChatRelay
{
    public class DiscordMessageFactory
    {
        //var json = "{\"content\":\"Incoming! <@&554312082137546762> <@446048405844918272>\",\"tts\":false,\"embed\":{\"title\":\"" + msg.Message + "\",\"description\":\"This message was sent from Terraria.\"}}";

        /// <summary>
        /// Returns an OpCode 2 JSON string that provides an authentication string.
        /// </summary>
        /// <param name="BOT_TOKEN">Token of bot to login with.</param>
        /// <returns>JSON to authenticate with.</returns>
        public static string CreateLogin(string BOT_TOKEN)
        {
            return "{\"op\":2,\"d\":{\"token\":\"" + BOT_TOKEN + "\",\"properties\":{\"$os\":\"linux\",\"$browser\":\"app\",\"$device\":\"mono\"},\"compress\":false}}";
        }

        /// <summary>
        /// Returns an OpCode 1 JSON string that can be used to send a heartbeat.
        /// </summary>
        /// <param name="LastSequenceNumber">Number from last JSON response to send back to server.</param>
        /// <returns>JSON to send heartbeat with.</returns>
        public static string CreateHeartbeat(int? LastSequenceNumber)
        {
            return "{\"op\": 1,\"d\": \"" + LastSequenceNumber + "\"}";
        }

        /// <summary>
        /// Returns a JSON string that can be used to send a text message.
        /// </summary>
        /// <param name="LastSequenceNumber">Number from last JSON response to send back to server.</param>
        /// <returns>JSON to send heartbeat with.</returns>
        public static string CreateTextMessage(string msg)
        {
            return "{\"content\":\"" + msg + "\",\"tts\":false}";
        }

        /// <summary>
        /// Attempts to convert the JSON string to a DiscordMessage. A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="json">JSON string to attempt to parse into a DiscordMessage.</param>
        /// <returns>Equivalent DiscordMessage</returns>
        public static bool TryParseMessage(string json, out Message msg)
        {
            try
            {
                msg =  JsonConvert.DeserializeObject<Message>(json);
                return true;
            }
            catch(JsonSerializationException e)
            {
                msg = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the JSON string to a DiscordDispatchMessage. A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="json">JSON string to attempt to parse into a DiscordDispatchMessage.</param>
        /// <returns>Equivalent DiscordDispatchMessage</returns>
        public static bool TryParseDispatchMessage(string json, out DispatchMessage msg)
        {
            try
            {
                var rawmsg = JsonConvert.DeserializeObject<Message>(json);

                if (rawmsg.OpCode == 0)
                {
                    msg = JsonConvert.DeserializeObject<DispatchMessage>(json);
                    return true;
                }

                msg = null;
                return false;
            }
            catch (JsonSerializationException e)
            {
                msg = null;
                return false;
            }
        }
    }
}
