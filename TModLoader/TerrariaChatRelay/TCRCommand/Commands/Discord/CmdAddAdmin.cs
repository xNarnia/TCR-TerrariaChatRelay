using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay.TCRCommand.Commands.Discord
{
	[Command]
	public class CmdAddAdmin : ICommand
	{
		public string Name { get; } = "Add Admin";

		public string CommandKey { get; } = "addadmin";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Grants the user access to Administrator level commands.";

		public string Usage { get; } = "addadmin @DiscordUser";

		public Permission DefaultPermissionLevel { get; } = Permission.Owner;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.Replace("<", "");
			input = input.Replace("@", "");
			input = input.Replace("!", "");
			input = input.Replace(">", "");

			if (ulong.TryParse(input, out ulong userId))
			{
				DiscordPlugin.Config.AdminUserIds.Add(userId);
				DiscordPlugin.Config.SaveJson();
				return "User successfully added.";
			}
			else
			{
				return "Could not find user. Example: addadmin @UserToGivePermissions " + input;
			}
		}
	}
}
