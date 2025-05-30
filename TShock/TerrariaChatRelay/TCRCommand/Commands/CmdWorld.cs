using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.TCRCommand.Commands
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

			int totalSeconds = (int)Game.World.GetTime();
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            string amPm = hours < 12 ? "AM" : "PM";
            string emoji = hours > 4.5 && hours < 19.5 ? "☀️" : "🌙";
            int hours12 = hours % 12;
            hours12 = hours12 == 0 ? 12 : hours12;
            string timeFormatted = $"{emoji} {hours12:D2}:{minutes:D2} {amPm}";

			worldinfo.Append("</b>Information about the currently running world</b> </br>");
			worldinfo.Append($"</box>World Name: {Game.World.GetName()} </br>");
			worldinfo.Append($"Time: {timeFormatted} </br>");
			worldinfo.Append($"Evil: {Game.World.GetEvilType()} </br>");
#if TSHOCK
			worldinfo.Append($"Difficulty: {(Game.World.IsMasterMode() ? "Master" : (Game.World.IsExpertMode() ? "Expert" : "Normal"))} </br>");
#endif
			worldinfo.Append($"Hardmode: {(Game.World.IsHardMode() ? "Yes" : "No")} </br>");
			worldinfo.Append($"World Size: {Game.World.getWorldSize()}");

#if TSHOCK
			if(Global.Config.ShowWorldSeed)
				worldinfo.Append($"</br>World Seed : {Game.World.GetWorldSeed()}");
#endif

			worldinfo.Append("</box>");
			return worldinfo.ToString();
		}
	}
}
