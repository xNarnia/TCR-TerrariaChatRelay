using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using Terraria.ModLoader.Config;
using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay
{
	public class Config : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header("General")]
		[DefaultValue("t!")]
		[ReloadRequired]
		public string CommandPrefix { get; set; }

		[DefaultValue("TerrariaChatRelay")]
		public string BotStatus { get; set; }

		[DefaultValue("%worldname% - **Players Online:** %playercount% / %maxplayers%")]
		public string BotChannelDescription { get; set; }

		[DefaultValue(true)]
		public bool ShowPoweredByMessageOnStartup { get; set; }

		[DefaultValue(5)]
		[ReloadRequired]
		public int NumberOfTimesToRetryConnectionAfterError { get; set; }

		[DefaultValue(10)]
		[ReloadRequired]
		public int SecondsToWaitBeforeRetryingAgain { get; set; }

		[Header("UserManagement")]
		[DefaultValue("0")]
		[ReloadRequired]
		public string OwnerUserId { get; set; }

		[ReloadRequired]
		[DefaultListValue("0")]
		public List<string> ManagerUserIds { get; set; } = new List<string>();

		[ReloadRequired]
		[DefaultListValue("0")]
		public List<string> AdminUserIds { get; set; } = new List<string>();

		[JsonIgnore]
		[ShowDespiteJsonIgnore]
		public bool OpenDiscordTokenGuide
		{
			get
			{
				return false;
			}
			set
			{
				if (value)
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = "https://github.com/xNarnia/TCR-TerrariaChatRelay/wiki/Discord-Relay-Setup",
						UseShellExecute = true
					});
				}
			}
		}

		public List<Endpoint> Endpoints { get; set; } = new List<Endpoint>();

		[Header("Formatting")]
		[JsonIgnore]
		[ShowDespiteJsonIgnore]
		public bool HoverHereForFormatHelp { get { return false; } set { } }

		[DefaultValue("[c/7489d8:Discord] - ")]
		[ReloadRequired]
		public string TerrariaInGameDiscordPrefix { get; set; }

		[DefaultValue("> **%playername%:** %message%")]
		[ReloadRequired]
		public string PlayerChatFormat { get; set; }

		[DefaultValue(":small_blue_diamond: **%playername%** joined the server.")]
		[ReloadRequired]
		public string PlayerLoggedInFormat { get; set; }

		[DefaultValue(":small_orange_diamond: **%playername%** left the server.")]
		[ReloadRequired]
		public string PlayerLoggedOutFormat { get; set; }

		[DefaultValue("**%message%**")]
		[ReloadRequired]
		public string WorldEventFormat { get; set; }

		[DefaultValue(":small_blue_diamond: **%message%**")]
		[ReloadRequired]
		public string ServerStartingFormat { get; set; }

		[DefaultValue(":small_orange_diamond: **%message%**")]
		[ReloadRequired]
		public string ServerStoppingFormat { get; set; }

		[DefaultValue(":anger: **%bossname% has awoken!**")]
		[ReloadRequired]
		public string VanillaBossSpawned { get; set; }

		public List<string> HideMessagesWithString { get; set; } = new List<string>();

		[JsonIgnore]
		[ShowDespiteJsonIgnore]
		[Header("StillNeedHelp")]
		public bool OpenDiscordSupportServer
		{
			get
			{
				return false;
			}
			set
			{
				if (value)
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = "https://discord.gg/xAQGT4VetN",
						UseShellExecute = true
					});
				}
			}
		}

		[JsonIgnore]
		[ShowDespiteJsonIgnore]
		public bool SpecialThanks { get { return false; } set { } }

		public Config()
		{
			Endpoints.Add(new Endpoint
			{
				BotToken = "BOT_TOKEN",
				Channel_IDs = [ 0 ],
				DenyReceivingMessagesFromGame = new List<ulong> { 0 },
				DenySendingMessagesToGame = new List<ulong> { 0 }
			});
		}
	}
}