using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay
{
	internal class PrettyPrint
	{
		internal static void Log(string text, ConsoleColor? msgForegroundColor = null, ConsoleColor? msgBackgroundColor = null)
			=> TCRCore.Helpers.PrettyPrint.Log("Adapter", text, msgForegroundColor, msgBackgroundColor);
	}
}
