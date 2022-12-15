using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRDiscord
{
	internal class PrettyPrint
	{
		internal static void Log(string text, ConsoleColor? msgForegroundColor = null, ConsoleColor? msgBackgroundColor = null)
			=> TCRCore.Helpers.PrettyPrint.Log("Discord", text, msgForegroundColor, msgBackgroundColor);
	}
}
