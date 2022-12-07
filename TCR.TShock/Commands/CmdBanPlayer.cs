using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI.DB;

namespace TerrariaChatRelay.Command.Commands
{
	[Command]
	public class CmdBanPlayer : ICommand
	{
		public string Name { get; } = "Ban Player";

		public string CommandKey { get; } = "ban";

		public string Description { get; } = "Bans the specified player. (Careful not to trigger other Discord bots!)";

		public string Usage { get; } = "ban PlayerName, silent/loud, reason, duration(s/m/d)";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.ToLower();

			if (input == null || input == "")
				return "Specify a player to ban. Example: ban AnOnlinePlayer, s/l, hacking, 2d";

			String[] spearator = { ", " };
			Int32 count = 4;
			String[] parameters = input.Split(spearator, count, StringSplitOptions.RemoveEmptyEntries);

			AddBanResult banResult;
			string reason = "Banned.";
			string duration = null;
			var result = new System.Text.StringBuilder();
			DateTime expiration = DateTime.MaxValue;


			if(parameters.Count() < 3)
            {
				return "Invalid syntax. Example : ban AnOnlinePlayer, s/l, hacking, 2d";
            }

			if (parameters.Count() == 4)
			{
				duration = parameters[3];
			}

			if (parameters.Count() >= 3)
			{
				reason = parameters[2];
			}

			if (TShockAPI.TShock.Utils.TryParseTime(duration, out int seconds))
			{
				expiration = DateTime.UtcNow.AddSeconds(seconds);
			}

			var players = TShockAPI.TSPlayer.FindByNameOrID(parameters[0]);
			if (players.Count > 1)
			{
				return "Found more than one matching players.";
			}

			if (players.Count < 1)
			{
				return "Could not find the target specified. Check that you have the correct spelling.";
			}
			var player = players[0];
			string[] identifier = { $"acc:{player.Name}", $"ip:{player.IP}", $"uuid:{player.UUID}", $"name:{player.Account.Name}"};
            
			for (int i=0; i<=3; i++)
            {
				banResult = TShockAPI.TShock.Bans.InsertBan(identifier[i], reason, $"TerrariaChatRelay : {whoRanCommand.Username}", DateTime.UtcNow, expiration);
				if (parameters[1] == "loud" || parameters[1] == "l")
				{
					if (banResult.Ban != null)
					{
						result.Append($"Ban added. Ticket Number {banResult.Ban.TicketNumber} was created for identifier {identifier[i]}.\n");
					}
					else
					{
						result.Append($"Failed to add ban for identifier: {identifier[i]}\n");
						result.Append($"Reason: {banResult.Message}\n");
					}
				}
			}
			player.Disconnect($"You have been banned: {reason}.\n");

			if (parameters[1] == "silent" || parameters[1] == "s")
				return "Command Executed!";

			return result.ToString();
		}
	}
}
