using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaChatRelay;
using TerrariaChatRelay.Command;

namespace TCRExampleCommand.Commands
{
	[Command]
	public class CmdExampleCommand : ICommand
	{
		public string Name { get; } = "Example Command";

		public string CommandKey { get; } = "examplecommand";

		public string[] Aliases { get; } = { "example", "ex" };

		public string Description { get; } = "Outputs example text";

		public string Usage { get; } = "example Output any text here!";

		/// <summary>
		/// Greater numbers mean higher permissions.
		/// User = 0, Manager = 1, Admin = 2, Owner = 3
		/// </summary>
		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			string output = "";

			output += $"</b>User entered:</b> {(input ?? "No input found!")}\n";

			output += $"</b>Your TShock server name is:</b> {TShockAPI.TShock.Config.Settings.ServerName}\n";

			output += $"</b>Your config's ExampleHelp variable says:</b> {Plugin.Config.ExampleHelp}\n";

			Plugin.Config.ExampleHelp = "ExampleCommand updated this!";
			Plugin.Config.SaveJson();

			output += $"</b>Your config's ExampleHelp variable says:</b> {Plugin.Config.ExampleHelp}";

			return output;

			// Example on how to make parameters!
			//if (parameters.Count() < 3)
			//{
			//    return "Invalid syntax. Example : myCommand param1, param2, param3, param4";
			//}

			//if (parameters.Count() == 4)
			//{
			//    duration = parameters[3];
			//}

			//if (parameters.Count() >= 3)
			//{
			//    reason = parameters[2];
			//}
		}
	}
}
