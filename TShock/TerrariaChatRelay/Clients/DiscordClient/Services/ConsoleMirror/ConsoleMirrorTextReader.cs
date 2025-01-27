using System;
using System.IO;
using System.Text;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.ConsoleMirror
{
    /// <summary>
    /// Wraps the original Console TextWriter, intercepting output.
    /// </summary>
    internal class ConsoleMirrorTextReader : TextReader, IDisposable
    {
        public event Action<String> ConsoleInputReceived;
        private TextReader previousReader { get; set; }

        public ConsoleMirrorTextReader(TextReader readerToWrap) 
        {
            previousReader = readerToWrap;
        }

        public override string ReadLine()
        {
            string line = previousReader.ReadLine();

            if (line != null)
                ConsoleInputReceived?.Invoke("> " + line);

            return line;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.SetIn(previousReader);
                base.Dispose(disposing);
            }

            base.Dispose(disposing);
        }
    }
}
