using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
//using Terraria.ModLoader;

namespace TerrariaChatRelay
{
	public class Command
	{
		public string CommandText { get; set; }
		public Action<string> Execute { get; set; }
	}

	//public class TCRCommandCaller : CommandCaller
	//{
	//	public CommandType CommandType => CommandType.Console;

	//	public Player Player => null;

	//	public void Reply(string text, Color color = default(Color))
	//	{
	//		string[] array = text.Split('\n');
	//		foreach (string value in array)
	//		{
	//			EventManager.RaiseTerrariaMessageReceived(null, -1, Color.Aqua, value);
	//		}
	//	}
	//}

	public static class CmdManager
	{
		//public static List<Command> BaseCommands = new List<Command>()
		//{
		//	new Command()
		//	{
		//		CommandText = "cmd",
		//		Execute = (msg) => 
		//		{
		//			TerrariaExtension.ExecuteCommand(msg, new TCRCommandCaller());
		//		}
		//	}
		//};

		//public static Command GetCommand(string msg)
		//{
		//	foreach(var cmd in BaseCommands)
		//	{
		//		if (msg.StartsWith(cmd.CommandText))
		//		{

		//		}
		//	}
		//}
	}
}
