namespace TCRCore.Command.Commands
{
	[Command]
	public class CmdWorld : ICommand
	{
		public string Name { get; } = "World Info";

		public string CommandKey { get; } = "world";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Displays the world info!";

		public string Usage { get; } = "world";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			var worldinfo = new System.Text.StringBuilder();

			worldinfo.Append("</b>Information about the currently running world</b> </br>");
			worldinfo.Append($"</box>World Name: {TCRCore.Game.World.GetName()} </br>");
			worldinfo.Append($"Evil: {TCRCore.Game.World.GetEvilType()} </br>");
#if TSHOCK
			worldinfo.Append($"Difficulty: {(TerrariaChatRelay.Game.World.IsMasterMode() ? "Master" : (TerrariaChatRelay.Game.World.IsExpertMode() ? "Expert" : "Normal"))} </br>");
#endif
			worldinfo.Append($"Hardmode: {(TCRCore.Game.World.IsHardMode() ? "Yes" : "No")} </br>");
			worldinfo.Append($"World Size: {TCRCore.Game.World.getWorldSize()}");

#if TSHOCK
			if(Global.Config.ShowWorldSeed)
				worldinfo.Append($"</br>World Seed : {TerrariaChatRelay.Game.World.GetWorldSeed()}");
#endif

			worldinfo.Append("</box>");
			return worldinfo.ToString();
		}
	}
}
