using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.DiscordClient.Messaging;
using TerrariaChatRelay.Clients.DiscordClient.Services.ConsoleMirror;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.Clients.DiscordClient.Services
{
    public class ConsoleMirrorService : IDiscordService
    {
        public List<ulong> ConsoleChannelIds { get; set; }
        public DiscordMessageQueue MessageQueue { get; set; }
        private ConsoleMirrorTextReader reader { get; set; }
        private ConsoleMirrorTextWriter writer { get; set; }
        private DiscordChatClient parentClient { get; }

        public ConsoleMirrorService(DiscordChatClient client, List<ulong> consoleChannelIds)
        {
            ConsoleChannelIds = consoleChannelIds ?? new List<ulong>();
            MessageQueue = new DiscordMessageQueue(1000);
            parentClient = client;
        }

        private void ConsoleMessageReceived(string output)
        {
            foreach (var channelid in ConsoleChannelIds)
            {
                MessageQueue.QueueMessage(channelid, new DiscordMessage()
                {
                    Message = output,
                    Embed = false
                });
            }
        }

        private void MessageQueue_OnReadyToSend(Dictionary<ulong, Queue<DiscordMessage>> messages)
        {
            foreach (var queue in messages)
            {
                string output = "";

                foreach (var msg in queue.Value)
                {
                    output += msg.Message + '\n';
                }

                int characterLimit = (2000 - "```powershell\n\n```".Length);

                if (output.Length > characterLimit)
                {
                    var splits = Split(output, characterLimit);
                    foreach(var content in splits)
                    {
                        parentClient.SendMessageToClient($"```powershell\n{content}\n```", null, queue.Key.ToString());
                    }
                }
                else
                {
                    parentClient.SendMessageToClient($"```powershell\n{output}\n```", null, queue.Key.ToString());
                }
            }
        }
        public IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public void Start()
        {
            if (ConsoleChannelIds.Count == 0)
                return;

            reader = new ConsoleMirrorTextReader(Console.In);
            reader.ConsoleInputReceived += ConsoleMessageReceived;
            writer = new ConsoleMirrorTextWriter(Console.Out);
            writer.ConsoleMessageReceived += ConsoleMessageReceived;
            MessageQueue.OnReadyToSend += MessageQueue_OnReadyToSend;
            Console.SetIn(reader);
            Console.SetOut(writer);
        }

        public void Stop()
        {
            try
            {
                reader.ConsoleInputReceived -= ConsoleMessageReceived;
                writer.ConsoleMessageReceived -= ConsoleMessageReceived;
                MessageQueue.OnReadyToSend -= MessageQueue_OnReadyToSend;
            }
            catch (Exception e)
            {
                PrettyPrint.Log("Discord", "Error stopping ConsoleMirrorService. Reason: " + e.Message);
            }

            writer?.Dispose();
            reader?.Dispose();
        }

        public void Dispose()
        {
            writer?.Dispose();
            reader?.Dispose();
        }
    }
}
