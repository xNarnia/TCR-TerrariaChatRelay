using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Command;
using TerrariaChatRelay.Helpers;
using TerrariaChatRelay.Clients.DiscordClient;
using Terraria;
using System.IO;
using Microsoft.Build.Tasks;

namespace TerrariaChatRelay.Command.Commands.Discord
{
	[Command]
	public class CmdUnbanManager : ICommand
	{
		public string Name { get; } = "Unban Player";

		public string CommandKey { get; } = "unban";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Unbans the specified player.";

		public string Usage { get; } = "unban PlayerName";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			var force = false;
			var playerName = input;
			input = input.ToLower();
			if (input == null || input == "")
				return "Specify a player to unban. Example: \"unban bannedPlayer\"";

			if(input.EndsWith(" --force"))
			{
				force = true;
				input = input.Substring(0, input.Length - 8);
			}
			string[] banFileContent = File.ReadAllLines(Netplay.BanFilePath);

			if (File.Exists(Netplay.BanFilePath))
			{
				var numberOfPlayersFound = 0;
				var currentLine = 0;
				var playerFoundAtLine = 0;
				foreach(var line in banFileContent)
				{
					if (line.ToLower() == $"//{input}")
					{
						numberOfPlayersFound++;
						playerFoundAtLine = currentLine;
					}
					currentLine++;
				}

				if (numberOfPlayersFound == 1)
				{
					banFileContent[playerFoundAtLine] = "";
					banFileContent[playerFoundAtLine + 1] = "";
					File.WriteAllLines(Netplay.BanFilePath, banFileContent);

					return $"Player {playerName} unbanned.";
				}
				else if(numberOfPlayersFound > 1 && !force)
				{
					return $"{numberOfPlayersFound} players found! " +
						$"\nIf you wish to unban {numberOfPlayersFound} players, run the same command with --force at the end." +
						$"\nOtherwise, use the {DiscordPlugin.Config.CommandPrefix}banfile command to see the banfile.txt location.";
				}
				else if(numberOfPlayersFound > 1 && force)
				{
					bool deleteNextLine = false;
					for (var i = 0; i < banFileContent.Length; i++)
					{
						// When we find a line //PlayerName, the next line will be their IP address to remove.
						if (deleteNextLine)
						{
							banFileContent[i] = "";
							deleteNextLine = false;
						}
						else if (banFileContent[i].ToLower() == $"//{input}")
						{
							banFileContent[i] = "";
							deleteNextLine = true;
						}
					}

					File.WriteAllLines(Netplay.BanFilePath, banFileContent);

					return $"{numberOfPlayersFound} players unbanned.";
				}
				else
				{
					return "Banned player not found.";
				}
			}

			return "Player not found.";
		}
	}
}
