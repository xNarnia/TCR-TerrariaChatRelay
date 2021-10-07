namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdWorld : ICommand
	{
		public string Name { get; } = "World Info";

		public string CommandKey { get; } = "world";

		public string Description { get; } = "Displays the world info!";

		public string Usage { get; } = "world";

		public Permission DefaultPermissionLevel { get; } = Permission.User;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			var worldinfo = new System.Text.StringBuilder();

			worldinfo.Append("</b>Information about the currently running world</b> </br>");
			worldinfo.Append($"</box>World Name : {TerrariaChatRelay.Game.World.GetName()} </br>");
			worldinfo.Append($"Hardmode : {TerrariaChatRelay.Game.World.GetEvilType()} </br>");
#if TSHOCK
			worldinfo.Append($"Difficulty : {(TerrariaChatRelay.Game.World.IsMasterMode() ? "Master" : (TerrariaChatRelay.Game.World.IsExpertMode() ? "Expert" : "Normal"))} </br>");
#endif
			worldinfo.Append($"Hardmode : {(TerrariaChatRelay.Game.World.IsHardMode() ? "Yes" : "No")} </br>");
			worldinfo.Append($"World Size : {TerrariaChatRelay.Game.World.getWorldSize()}");

#if TSHOCK
			if(Global.Config.ShowWorldSeed)
				worldinfo.Append($"</br>World Seed : {TerrariaChatRelay.Game.World.GetWorldSeed()}");
#endif

			worldinfo.Append("</box>");
			return worldinfo.ToString();
		}
	}
}
