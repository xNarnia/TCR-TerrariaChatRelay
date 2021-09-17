using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI.Chat;
using TerrariaChatRelay.Clients.Interfaces;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay
{
    public class EventManager
    {
        /// <summary>
        /// IChatClients list for clients to register with.
        /// </summary>
        public static List<IChatClient> Subscribers { get; set; }

        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageReceived;
        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageSent;

		/// <summary>
		/// Intializes all values to default values to ready EventManager for use.
		/// </summary>
		public static void Initialize()
		{
			Subscribers = new List<IChatClient>();
		}

		/// <summary>
		/// Emits a message to all subscribers that a game message has been received with Color.White.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="msg">Text content of the message</param>
		public static void RaiseTerrariaMessageReceived(object sender, int playerId, string msg)
			=> RaiseTerrariaMessageReceived(sender, playerId, Color.White, msg);

		/// <summary>
		/// Emits a message to all subscribers that a game message has been received.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// <param name="msg">Text content of the message</param>
		public static void RaiseTerrariaMessageReceived(object sender, int playerId, Color color, string msg)
        {
            var snippets = ChatManager.ParseMessage(msg, color);

            string outmsg = "";
            foreach (var snippet in snippets)
            {
                outmsg += snippet.Text;
            }

            OnGameMessageReceived?.Invoke(sender, new TerrariaChatEventArgs(playerId, color, outmsg));
        }

		public static void ConnectClients()
        {
			PrettyPrint.Log("Connecting clients...");

			for (var i = 0; i < Subscribers.Count; i++)
			{
				PrettyPrint.Log(Subscribers[i].GetType().ToString() + " Connecting...");
				Subscribers[i].Connect();
            }
			Console.ResetColor();
        }

        public static void DisconnectClients()
        {
            foreach (var subscriber in Subscribers)
            {
                subscriber.Disconnect();
            }

			Subscribers.Clear();
        }
    }

    public class TerrariaChatEventArgs : EventArgs
    {
        public Player Player { get; set; }
        public Color Color { get; set; }
        public string Message { get; set; }
		public int ID { get; set; }

		/// <summary>
		/// Message payload sent to subscribers when a game message has been received.
		/// </summary>
		/// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// <param name="msg">Text content of the message</param>
		public TerrariaChatEventArgs(int playerId, Color color, string msg)
        {
			if(playerId >= 0)
				Player = Terraria.Main.player[playerId] ?? null;
			Color = color;
            Message = msg;
			ID = playerId;
		}
    }
}
