using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.Command.Commands.Discord
{
	[Command]
	public class CmdRestart : ICommand
	{
		public string Name { get; } = "Restart";

		public string CommandKey { get; } = "restart";

		public string[] Aliases { get; } = { "reset", "reboot" };

		public string Description { get; } = "Restarts the bot.";

		public string Usage { get; } = "restart";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			Task.Run(async () =>
			{
				await Task.Delay(1500);
				Core.DisconnectClients();
				Global.Config = new TCRConfig().GetOrCreateConfiguration();
				Core.ConnectClients();
			});

			return "Restarting bot...";
		}
	}
}
