using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI.Chat;
using TerrariaChatRelay.Clients;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Helpers;
using TerrariaChatRelay.Models;

namespace TerrariaChatRelay
{
    public class Core
    {
        /// <summary>
        /// IChatClients list for clients to register with.
        /// </summary>
        public static List<IChatClient> Subscribers { get; set; }
		public static ICommandService CommandServ { get; set; }

        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageReceived;
        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageSent;

		public static event EventHandler<ClientChatEventArgs> OnClientMessageReceived;
		public static event EventHandler<ClientChatEventArgs> OnClientMessageSent;

		private static ITCRAdapter _adapter;

		~Core()
		{
			_adapter = null;
		}

		/// <summary>
		/// Intializes all values to default values to ready EventManager for use.
		/// </summary>
		public static void Initialize(ITCRAdapter adapter)
		{
			Subscribers = new List<IChatClient>();
			CommandServ = new CommandService();
			_adapter = adapter;
		}

		/// <summary>
		/// Emits a message to TerrariaChatRelay that a client message have been received.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="user">User object detailing the client source and username</param>
		/// <param name="msg">Text content of the message</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
		public static void RaiseClientMessageReceived(object sender, TCRClientUser user, string msg, string commandPrefix, ulong sourceChannelId = 0)
			=> RaiseClientMessageReceived(sender, user, "", msg, commandPrefix, sourceChannelId);

		/// <summary>
		/// Emits a message to TerrariaChatRelay that a client message have been received.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="user">User object detailing the client source and username</param>
		/// <param name="msg">Text content of the message</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <param name="clientPrefix">String to insert before the main chat message.</param>
		/// <param name="sourceChannelId">Optional id for clients that require id's to send to channels. Id of the channel the message originated from.</param>
		public static void RaiseClientMessageReceived(object sender, TCRClientUser user, string clientPrefix, string msg, string commandPrefix, ulong sourceChannelId = 0)
		{
			if(CommandServ.IsCommand(msg, commandPrefix))
			{
				var payload = CommandServ.GetExecutableCommand(msg, commandPrefix, user);
				msg = payload.Execute();
				((IChatClient)sender).HandleCommand(payload, msg, sourceChannelId);
			}
			else
			{
				_adapter.BroadcastChatMessage($"{clientPrefix}<{user.Username}> {msg}", -1);
				OnClientMessageReceived?.Invoke(sender, new ClientChatEventArgs(user, msg));
			}
		}

		/// <summary>
		/// Emits a message to all subscribers that a game message has been received with Color.White.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="msg">Text content of the message</param>
		public static void RaiseTerrariaMessageReceived(object sender, TCRPlayer player, string msg)
		{
			RaiseTerrariaMessageReceived(sender, player, new TCRColor(255, 255, 255), _adapter.ParseSnippets(msg));
		}

		/// <summary>
		/// Emits a message to all subscribers that a game message has been received.
		/// </summary>
		/// <param name="sender">Object that is emitting this event.</param>
		/// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// <param name="msg">Text content of the message</param>
		public static void RaiseTerrariaMessageReceived(object sender, TCRPlayer player, TCRColor color, string msg)
            => OnGameMessageReceived?.Invoke(sender, new TerrariaChatEventArgs(player, color, msg));

		public static void ConnectClients()
        {
			PrettyPrint.Log("Connecting clients...");
			Console.WriteLine("1");

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var references = assembly.GetReferencedAssemblies().ToList();

				foreach (var reference in references)
				{
					if (reference.Name.Contains("TCR"))
					{
						foreach (var type in assembly.GetTypes())
						{
							if (type.BaseType == typeof(TCRPlugin))
							{
								Activator.CreateInstance(type);
								break;
							}
						}

						((CommandService)CommandServ).ScanForCommands(assembly);
						break;
					}
				}
			}
			Console.WriteLine("3");

			for (var i = 0; i < Subscribers.Count; i++)
			{
				PrettyPrint.Log(Subscribers[i].GetType().ToString() + " Connecting...");
				Subscribers[i].ConnectAsync();
            }
			Console.ResetColor();
        }

        public static void DisconnectClients()
        {
			var i = 0;
            while (i < Subscribers.Count)
            {
				i++;
				var subcriberName = Subscribers[0].GetType().ToString();
				try
				{
					Subscribers[0].Disconnect();
					PrettyPrint.Log(subcriberName, $"Disconnecting...");
				}
				catch (Exception)
				{
					PrettyPrint.Log(subcriberName, "Failed to disconnect!");
				}

				try
				{
					Subscribers[0].Dispose();
					PrettyPrint.Log(subcriberName, $"Disposing...");
				}
				catch (Exception)
				{
					PrettyPrint.Log(subcriberName, "Failed to dispose!");
				}
			}

			Subscribers.Clear();
        }
    }

    public class TerrariaChatEventArgs : EventArgs
    {
        public TCRPlayer Player { get; set; }
        public TCRColor Color { get; set; }
        public string Message { get; set; }

		/// <summary>
		/// Message payload sent to subscribers when a game message has been received.
		/// </summary>
		/// <param name="player">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// 
		/// <param name="msg">Text content of the message</param>
		public TerrariaChatEventArgs(TCRPlayer player, TCRColor color, string msg)
        {
			Player = player;
			Color = color;
            Message = msg;
		}
    }

	public class ClientChatEventArgs : EventArgs
	{
		public TCRClientUser User { get; set; }
		public string Message { get; set; }

		/// <summary>
		/// Message payload sent to subscribers when a game message has been received.
		/// </summary>
		/// <param name="player">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
		/// <param name="color">Color to display the text.</param>
		/// <param name="msg">Text content of the message</param>
		public ClientChatEventArgs(TCRClientUser user, string msg)
		{
			User = user;
			Message = msg;
		}
	}
}
