using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCRCore;
using TCRCore.Helpers;

namespace TCRSlack
{
	public class Configuration : SimpleConfig<Configuration>
	{
		public override string FileName { get; set; }
			= Path.Combine(Global.ModConfigPath, "TCR.Slack.json");

		[JsonProperty(Order = 10)]
		public string CommentGuide { get; set; } = "Setup Guide: https://tinyurl.com/TCR-Setup";
		[JsonProperty(Order = 40)]
		public bool EnableSlack { get; set; } = true;
		[JsonProperty(Order = 50)]
		public string CommandPrefix { get; set; } = "t!";
		[JsonProperty(Order = 60)]
		public bool ShowPoweredByMessageOnStartup { get; set; } = true;
		[JsonProperty(Order = 75)]
		public string OwnerUser { get; set; } = "";
		[JsonProperty(Order = 110)]
		public List<string> ManagerUsers { get; set; } = new List<string>();
		[JsonProperty(Order = 120)]
		public List<string> AdminUsers { get; set; } = new List<string>();
		[JsonProperty(Order = 130)]
		public string EndpointHelp1 { get; set; } = "Use commands to handle denying sending and receiving! Use 't!help admin' for more information";
		[JsonProperty(Order = 133)]
		public List<Endpoint> EndPoints { get; set; } = new List<Endpoint>();
		[JsonProperty(Order = 134)]
		public string GameStatusHelp1 { get; set; } = "Sets the bots now playing status. Can be configured with variables";
		[JsonProperty(Order = 135)]
		public string GameStatusHelp2 { get; set; } = "%playercount% = Number of current players on the server";
		[JsonProperty(Order = 136)]
		public string GameStatusHelp3 { get; set; } = "%maxplayers% = Maximum number of players allowed, based on serverconfig.txt or command prompt entry";
		[JsonProperty(Order = 138)]
		public string GameStatus { get; set; } = "with %playercount%/%maxplayers% players!";
		[JsonProperty(Order = 140)]
		public string RetryHelp { get; set; } = "Set NumberOfTimesToRetryConnectionAfterError to -1 to retry infinitely";
		[JsonProperty(Order = 150)]
		public int NumberOfTimesToRetryConnectionAfterError { get; set; } = 5;
		[JsonProperty(Order = 160)]
		public int SecondsToWaitBeforeRetryingAgain { get; set; } = 10;
		[JsonProperty(Order = 170)]
		public string FormatHelp1 { get; set; } = "You can insert any of these formatters to change how your message looks! (CASE SENSITIVE)";
		[JsonProperty(Order = 180)]
		public string FormatHelp2 { get; set; } = "%playername% = Player Name";
		[JsonProperty(Order = 190)]
		public string FormatHelp3 { get; set; } = "%worldname% = World Name";
		[JsonProperty(Order = 200)]
		public string FormatHelp4 { get; set; } = "%message% = Initial message content";
		[JsonProperty(Order = 210)]
		public string FormatHelp5 { get; set; } = "%bossname% = Name of boss being summoned (only for VanillaBossSpawned)";
		[JsonProperty(Order = 220)]
		public string FormatHelp6 { get; set; } = "%groupprefix% = Group prefix";
		[JsonProperty(Order = 230)]
		public string FormatHelp7 { get; set; } = "%groupsuffix% = Group suffix";
		[JsonProperty(Order = 66)]
		public string RegexHelp1 { get; set; } = "For more advanced users, you can use RegexMessageReplace to modify/filter the final message being sent.";
		[JsonProperty(Order = 67)]
		public string RegexHelp2 { get; set; } = "Example (Filtering nth mob kill annoucements): { \"^.+ has defeated the \\d+th .+$\": \"\" }";

		[JsonProperty(Order = 235)]
		public static string TerrariaInGameSlackPrefix = "[c/2EB67D:S][c/32beb7:l][c/36c5f0:a][c/8b72a5:c][c/E01E5A:k] - ";
		[JsonProperty(Order = 240)]
		public static string PlayerChatFormat = "> *%playername%:* %message%";
		[JsonProperty(Order = 250)]
		public static string PlayerLoggedInFormat = ":small_blue_diamond: *%playername%* joined the server.";
		[JsonProperty(Order = 260)]
		public static string PlayerLoggedOutFormat = ":small_orange_diamond: *%playername%* left the server.";
		[JsonProperty(Order = 270)]
		public static string WorldEventFormat = "*%message%*";
		[JsonProperty(Order = 280)]
		public static string ServerStartingFormat = ":small_blue_diamond: *%message%*";
		[JsonProperty(Order = 290)]
		public static string ServerStoppingFormat = ":small_orange_diamond: *%message%*";
		[JsonProperty(Order = 305)]
		public static string VanillaBossSpawned = ":anger: *%bossname% has awoken!*";
		
		[JsonProperty(Order = 120)]
		public bool RegexMessageEnabled { get; set; } = false;
		[JsonProperty(Order = 125)]
		public Dictionary<string, string> RegexMessageReplace { get; set; } = new Dictionary<string, string>();


		public Configuration()
		{
			if (!File.Exists(FileName))
			{
				// Discord
				EndPoints.Add(new Endpoint());
				ManagerUsers.Add("");
				AdminUsers.Add("");

				// Regex
				RegexMessageReplace = new Dictionary<string, string>
                {
					["^(.*)$"] = "$1"
                };
			}
		}
	}

	public class Endpoint
	{
		public string xapp_BotToken { get; set; } = "xapp-BOT_TOKEN";
		public string xoxb_BotToken { get; set; } = "xoxb-BOT_TOKEN";
		public string[] Channel_IDs { get; set; } = { "general" };
		public List<string> DenySendingMessagesToGame { get; set; } = new List<string>();
		public List<string> DenyReceivingMessagesFromGame { get; set; } = new List<string>();
	}
}