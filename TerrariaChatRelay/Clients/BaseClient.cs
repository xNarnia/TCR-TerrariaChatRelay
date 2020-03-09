using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay.Clients
{
    public abstract class BaseClient : IChatClient
    {
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
            EventManager.OnGameMessageReceived += GameMessageReceivedHandler;
            EventManager.OnGameMessageSent += GameMessageSentHandler;
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
            EventManager.OnGameMessageReceived -= GameMessageReceivedHandler;
            EventManager.OnGameMessageSent -= GameMessageSentHandler;
        }

        public abstract void Connect();
        public abstract void Disconnect();

        // Events
        //public abstract Task ClientMessageReceived_Handler(string msg);
        //public abstract Task ClientMessageSent_Handler(string msg);
        public abstract void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg);
        public abstract void GameMessageSentHandler(object sender, TerrariaChatEventArgs msg);
    }
}
