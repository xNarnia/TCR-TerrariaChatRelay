using System.Linq;
using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay.TCRCommand.Commands.Discord
{
	[Command]
	public class CmdListManager : ICommand
	{
		public string Name { get; } = "List Manager";

		public string CommandKey { get; } = "listmanager";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Lists all users given access to Manager level commands.";

		public string Usage { get; } = "listmanager";

		public Permission DefaultPermissionLevel { get; } = Permission.Admin;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			var mentions = DiscordPlugin.Config.ManagerUserIds.Where(x => x != 0).ToList();
			if (mentions.Count() == 0)
			{
				return "**No administrators found!**";
			}
			return "**Administrators for TerrariaChatRelay:**\n" + string.Join("\n", DiscordPlugin.Config.ManagerUserIds.Where(x => x != 0).Select(x => $"<@{x}> - ID: {x}\n"));
		}
	}
}
