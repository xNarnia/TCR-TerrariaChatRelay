using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Command
{
	public interface ICommandService
	{
		/// <summary>
		/// Dictionary containing all the Commands, the keys of the dictionary matching the associated keys in the Command objects.
		/// </summary>
		Dictionary<string, ICommand> Commands { get; }

		/// <summary>
		/// Adds a command to the CommandService.
		/// </summary>
		/// <param name="command">Command to add to the CommandService.</param>
		void AddCommand(ICommand command);

		/// <summary>
		/// Returns whether the raw chat is an attempt to run a command.
		/// </summary>
		/// <param name="input">Raw chat message from the client user.</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <returns>True if a command key is found. False if not.</returns>
		bool IsCommand(string input, string commandPrefix);

		/// <summary>
		/// Parses raw chat message and returns an executable command payload.
		/// </summary>
		/// <param name="input">Raw chat message from client user.</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <param name="user">User that attempted to execute the command.</param>
		/// <returns>Command payload with associated data and user to run it.</returns>
		ICommandPayload GetExecutableCommand(string input, string commandPrefix, TCRClientUser user);

		/// <summary>
		/// Receives raw input from the game and checks whether it has an associated command. If it does, it checks if the user has permission to run it.
		/// </summary>
		/// <param name="commandRun">Command payload containing which command to run, as well as all necessary data to run it.</param>
		/// <returns>If successful, returns a response from the command's executor. If no permission, returns a message indicating so.</returns>
		string ExecuteCommand(ICommandPayload commandRun);
	}
}
