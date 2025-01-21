namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdVersion : ICommand
	{
		public string Name { get; } = "TCR Version";

		public string CommandKey { get; } = "version";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Displays the version of TCR!";

		public string Usage { get; } = "version";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			return $"</b>TerrariaChatRelay Version:</b> v{Core.TCRVersion}\n</b>TShock Version:</b> v{TShockAPI.TShock.VersionNum}";
		}
	}
}
