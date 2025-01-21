using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;

namespace TerrariaChatRelay.Command.Commands
{
	public class TCRCommandCaller : CommandCaller
	{
		public CommandType CommandType => CommandType.Console;

		public Player Player => null;

		public void Reply(string text, Color color)
		{
			string[] array = text.Split('\n');
			foreach (string value in array)
			{
				if (value.Length > 0)
					Core.RaiseTerrariaMessageReceived(this, TCRPlayer.Server, value);
			}
		}
	}
}
