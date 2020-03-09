using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.Interfaces
{
    public interface IChatClient
    {
        /// <summary>
        /// Initialize client to parent repo.
        /// </summary>
        /// <param name="parent">Parent repo to register with.</param>
        void Init(List<IChatClient> parent);
        
        /// <summary>
        /// Handle cleanup, de-register, and dispose client.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Initiate connection to service.
        /// </summary>
        void Connect();

        /// <summary>
        /// Terminate connection to service.
        /// </summary>
        void Disconnect();

        ///// <summary>
        ///// Handler fired when server receives a message from service.
        ///// </summary>
        //Task ClientMessageReceived_Handler(string msg);
        ///// <summary>
        ///// Handler fired when server sends a message to service.
        ///// </summary>
        //Task ClientMessageSent_Handler(string msg);
        ///// <summary>
        ///// Handler fired when server receives a message from the game.
        ///// </summary>
        void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg);
        ///// <summary>
        ///// Handler fired when server sends a message to the game.
        ///// </summary>
        void GameMessageSentHandler(object sender, TerrariaChatEventArgs msg);

    }
}
