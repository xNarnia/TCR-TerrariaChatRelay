using System;
using System.Collections.Generic;
using TerrariaChatRelay;
using TerrariaChatRelay.Clients;
using TerrariaChatRelay.Helpers;

namespace TCRExampleCommand
{
	public class Plugin : TCRPlugin
	{
		public static ExampleConfig Config { get; set; }

		public override void Init(List<IChatClient> Subscribers)
		{
			// Get a configuration file if it exists, or create a new one!
			Config = new ExampleConfig().GetOrCreateConfiguration();

			if (Config.EnableExampleCommand)
			{
				PrettyPrint.Log("TShock.ExampleCommand", "Example Command is Enabled!");

				// This will tell TCR to grab commands from this plugin when it is loaded.
				((CommandService)Core.CommandServ).ScanForCommands(this);

				// Now all you have to do is create a new command!
				// Check the Commands folder for examples.
			}
		}
	}
}