using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;

namespace TerrariaChatRelay.Clients
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
        void ConnectAsync();

        /// <summary>
        /// Terminate connection to service.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends a message to the client from TerrariaChatRelay
        /// </summary>
        /// <param name="msg">Text content of the message.</param>
        /// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
        void SendMessageToClient(string msg, ulong sourceChannelId = 0);

        /// <summary>
        /// Parses incoming result messages from already executed commands for the client to handle.
        /// </summary>
        /// <param name="payload">The payload that was ran before being sent to this handler. Boolean Executed updated to reflect whether it successfully executed or not.</param>
        /// <param name="msg">The output message from the command execution detailing the status of the command.</param>
        /// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
        void HandleCommand(ICommandPayload payload, string msg, ulong sourceChannelId);

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
