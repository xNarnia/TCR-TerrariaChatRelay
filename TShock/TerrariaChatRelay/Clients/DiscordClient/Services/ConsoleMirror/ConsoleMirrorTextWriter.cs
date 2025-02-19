using System;
using System.IO;
using System.Text;

namespace TerrariaChatRelay.Clients.DiscordClient.Services.ConsoleMirror
{
    /// <summary>
    /// Wraps the original Console TextWriter, intercepting output.
    /// </summary>
    internal class ConsoleMirrorTextWriter : TextWriter, IDisposable
    {
        public event Action<String> ConsoleMessageReceived;
        public override Encoding Encoding { get; } = Encoding.UTF8;
        private TextWriter previousWriter { get; set; }
        private string writeCatch { get; set; }

        public ConsoleMirrorTextWriter(TextWriter writerToWrap) 
        {
            previousWriter = writerToWrap;
            writeCatch = "";
        }

        public override void WriteLine(string value)
        {
            ConsoleMessageReceived?.Invoke(writeCatch + value);
            writeCatch = "";
            previousWriter.WriteLine(value);
        }

        public override void Write(char value)
        {
            writeCatch += value;
            previousWriter?.Write(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.SetOut(previousWriter);
                base.Dispose(disposing);
            }

            base.Dispose(disposing);
        }
    }
}
