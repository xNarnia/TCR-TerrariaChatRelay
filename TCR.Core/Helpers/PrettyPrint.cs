using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCRCore.Helpers
{
	public static class PrettyPrint
	{
		public static void Write(string text, ConsoleColor? foreground = null, ConsoleColor? background = null)
		{
			SetColor(foreground, background);

			Console.Write(text);
			Console.ResetColor();
		}

		public static void WriteLine(string text, ConsoleColor? foreground = null, ConsoleColor? background = null)
		{
			SetColor(foreground, background);

			Console.WriteLine(text);
			Console.ResetColor();
		}

		internal static void Log(string text, ConsoleColor? msgForegroundColor = null, ConsoleColor? msgBackgroundColor = null)
		{
			PrettyPrint.Write(" [TerrariaChatRelay]: ", ConsoleColor.DarkCyan);
			SetColor(msgForegroundColor, msgBackgroundColor);
			Console.WriteLine(text);
			Console.ResetColor();
		}

		public static void Log(string module, string text, ConsoleColor? msgForegroundColor = null, ConsoleColor? msgBackgroundColor = null)
		{
			PrettyPrint.Write($" [TerrariaChatRelay] | [{module}]: ", ConsoleColor.DarkCyan);
			SetColor(msgForegroundColor, msgBackgroundColor);
			Console.WriteLine(text);
			Console.ResetColor();
		}

		private static void SetColor(ConsoleColor? foreground = null, ConsoleColor? background = null)
		{
			Console.ResetColor();
			if (foreground != null)
				Console.ForegroundColor = foreground.Value;

			if (background != null)
				Console.BackgroundColor = background.Value;
		}
	}
}
