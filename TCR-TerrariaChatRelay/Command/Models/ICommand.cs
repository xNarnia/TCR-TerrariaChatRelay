using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;

namespace TerrariaChatRelay.Command
{
	public interface ICommand
	{
		/// <summary>
		/// The descriptive name of the command.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The sequence of characters sent from a client user that identifies this command to run.
		/// </summary>
		string CommandKey { get; }

		/// <summary>
		/// Summarized explanation of the command's functionality.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// String example of how to use the command. Example: !cmdkey params
		/// </summary>
		string Usage { get; }

		/// <summary>
		/// The permission level required to run this command if no permission is assigned.
		/// </summary>
		Permission DefaultPermissionLevel { get; }

		/// <summary>
		/// The desired action to be performed when the command key is sent.
		/// </summary>
		/// <param name="input">Parameters sent when the command was executed. Example: "!poke Narnia 50" would return "Narnia 50"</param>
		/// <param name="whoRanCommand">The client user who initiated the command.</param>
		/// <returns>Message result based on the command. Should be a message explaining the status of the execution.</returns>
		string Execute(string input = null, TCRClientUser whoRanCommand = null);
	}
}
