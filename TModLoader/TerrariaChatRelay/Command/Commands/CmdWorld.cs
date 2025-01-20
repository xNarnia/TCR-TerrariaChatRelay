using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.Command.Commands
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
			worldinfo.Append($"</box>World Name: {GameHelper.World.GetName()} </br>");
			worldinfo.Append($"Evil: {GameHelper.World.GetEvilType()} </br>");
#if TSHOCK
			worldinfo.Append($"Difficulty: {(TCRCore.Game.World.IsMasterMode() ? "Master" : (TCRCore.Game.World.IsExpertMode() ? "Expert" : "Normal"))} </br>");
#endif
			worldinfo.Append($"Hardmode: {(GameHelper.World.IsHardMode() ? "Yes" : "No")} </br>");
			worldinfo.Append($"World Size: {GameHelper.World.getWorldSize()}");

#if TSHOCK
			if(Global.Config.ShowWorldSeed)
				worldinfo.Append($"</br>World Seed : {TCRCore.Game.World.GetWorldSeed()}");
#endif

			worldinfo.Append("</box>");
			return worldinfo.ToString();
		}
	}
}
