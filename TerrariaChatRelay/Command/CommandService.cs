using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;

namespace TerrariaChatRelay
{
	public class CommandService : ICommandService
	{
		/// <summary>
		/// Dictionary containing all the Commands, the keys of the dictionary matching the associated keys in the Command objects.
		/// </summary>
		public Dictionary<string, ICommand> Commands { get; }

		/// <summary>
		/// Returns whether the raw chat is an attempt to run a command.
		/// </summary>
		/// <param name="input">Raw chat message from the client user.</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <returns>True if a command key is found. False if not.</returns>
		public bool IsCommand(string input, string commandPrefix)
		{
			string newinput = input.ToLower().Remove(0, commandPrefix.Length);

			int indexOfSeparator = newinput.IndexOf(' ');
			if (indexOfSeparator > 0)
				newinput = newinput.Substring(0, indexOfSeparator);

			return Commands.ContainsKey(newinput);
		}

		/// <summary>
		/// Adds a command to the CommandService.
		/// </summary>
		/// <param name="command">Command to add to the CommandService.</param>
		public void AddCommand(ICommand command)
			=> Commands.Add(command.CommandKey, command);

		/// <summary>
		/// Receives raw input from the game and checks whether it has an associated command. If it does, it checks if the user has permission to run it.
		/// </summary>
		/// <param name="payload">Command payload containing which command to run, as well as all necessary data to run it.</param>
		/// <returns>If successful, returns a response from the command's executor. If no permission, returns a message indicating so.</returns>
		public string ExecuteCommand(ICommandPayload payload)
		{
			if (payload.UserExecutor.PermissionLevel >= payload.Command.DefaultPermissionLevel)
				return payload.Command.Execute(payload.Parameters, payload.UserExecutor);
			else
				return "You don't have permission to use this command!";
		}

		/// <summary>
		/// Parses raw chat message and returns an executable command payload.
		/// </summary>
		/// <param name="input">Raw chat message from client user.</param>
		/// <param name="commandPrefix">Command prefix to indicate a command is being used.</param>
		/// <returns>Command payload with associated data and user to run it.</returns>
		public ICommandPayload GetExecutableCommand(string input, string commandPrefix, TCRClientUser user)
		{
			string commandKey = input.Remove(0, commandPrefix.Length);
			string parameters = "";

			int indexOfSeparator = commandKey.IndexOf(' ');
			if (indexOfSeparator > 0)
			{
				parameters = commandKey.Substring(indexOfSeparator + 1, commandKey.Length - indexOfSeparator - 1);
				commandKey = commandKey.Substring(0, indexOfSeparator);
			}

			return new CommandPayload(this, Commands[commandKey], parameters, user);
		}

		public void ScanForCommands(object caller)
		{
			
			foreach (Type type in Assembly.GetAssembly(caller.GetType()).GetTypes())
			{
				if (type.GetCustomAttributes(typeof(CommandAttribute), true).Length > 0)
				{
					try
					{
						AddCommand((ICommand)Activator.CreateInstance(type));
					}
					catch (Exception) { }
				}
			}
		}

		public CommandService()
		{
			Commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);
			ScanForCommands(this);
		}
	}
}