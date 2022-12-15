using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI.DB;

namespace TCRCore.Command.Commands
{
	[Command]
	public class CmdUnbanPlayer : ICommand
	{
		public string Name { get; } = "Unban Player";

		public string CommandKey { get; } = "unban";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Unbans the specified player.";

		public string Usage { get; } = "unban banID";

		public Permission DefaultPermissionLevel { get; } = Permission.Manager;

		public string Execute(object sender, string input = null, TCRClientUser whoRanCommand = null)
		{
			input = input.ToLower();
			if (!int.TryParse(input, out int banId))
			{
				return $"Invalid Ticket Number.";
			}

			if (TShockAPI.TShock.Bans.RemoveBan(banId))
			{
				TShockAPI.TShock.Log.ConsoleInfo($"Ban {banId} has been revoked by \"TerrariaRelayChat : {whoRanCommand.Username}\".");
				return $"Ban {banId} has now been marked as expired.";
			}

			return "";
		}
	}
}
