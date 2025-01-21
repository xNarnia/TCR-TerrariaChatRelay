using System.Collections.Generic;
using TerrariaChatRelay.Command;

namespace TerrariaChatRelay.Clients
{
    public abstract class BaseClient : IChatClient
    {
        public abstract string Name { get; set; }
        private List<IChatClient> _parent;
        private bool _disposed;

        /// <summary>
        /// Base class for IChatClients. Registers self into static ClientRepo.
        /// </summary>
        /// <param name="parent"></param>
        public BaseClient(List<IChatClient> parent)
        {
            _disposed = false;
            Init(parent);
        }

        /// <summary>
        /// Handle disposing of client.
        /// </summary>
        ~BaseClient()
        {
            Dispose();
        }

        /// <summary>
        /// Registers self to ClientRepo
        /// </summary>
        /// <param name="parent"></param>
        public void Init(List<IChatClient> parent)
        {
            _parent = parent;
            _parent.Add(this);

            //EventManager.OnClientMessageReceived += ClientMessageReceived_Handler;
            //EventManager.OnClientMessageSent += ClientMessageSent_Handler;
            Core.OnGameMessageReceived += GameMessageReceivedHandler;
        }

        /// <summary>
        /// De-registers self from ClientRepo and destroys events
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _parent.Remove(this);
            Core.OnGameMessageReceived -= GameMessageReceivedHandler;
        }

        public abstract void ConnectAsync();
        public abstract void Disconnect();


        /// <summary>
        /// Sends a message to the client from TerrariaChatRelay
        /// </summary>
        /// <param name="msg">Text content of the message.</param>
        /// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
        public abstract void SendMessageToClient(string msg, string sourceChannelId = "");
        /// <summary>
        /// Parses incoming result messages from already executed commands for the client to handle.
        /// </summary>
        /// <param name="payload">The payload that was ran before being sent to this handler. Boolean Executed updated to reflect whether it successfully executed or not.</param>
        /// <param name="msg">The output message from the command execution detailing the status of the command.</param>
        /// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
        public abstract void HandleCommandOutput(ICommandPayload payload, string msg, string sourceChannelId = "");

        // Events
        //public abstract Task ClientMessageReceived_Handler(string msg);
        //public abstract Task ClientMessageSent_Handler(string msg);
        public abstract void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg);
	}
}
