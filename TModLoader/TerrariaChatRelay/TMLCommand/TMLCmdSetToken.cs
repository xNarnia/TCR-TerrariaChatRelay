using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using TerrariaChatRelay.Clients.DiscordClient;
using System.Threading.Channels;

namespace TerrariaChatRelay.TMLCommand
{
	/*
	 This command will not work due to the input being ToLower()'ed.
	 Must wait for this PR.
	 https://github.com/tModLoader/tModLoader/pull/4514
	 */

	//public class TMLCmdSetToken : ModCommand
	//{
	//	public override CommandType Type
	//		=> CommandType.Console;

	//	// The desired text to trigger this command
	//	public override string Command
	//		=> "tcrToken";

	//	// A short usage explanation for this command
	//	public override string Usage
	//		=> "tcrtoken TOKENHERE - Sets the bot token" +
	//		"tcrtoken EndpointPosition TOKENHERE - Sets the bot token for the desired endpoint" +
	//		"   Example Usage: tcrtoken ABCDEFGHIJKLMNOPQRSTUVWXYZ.abc.123456789" +
	//		"   Example Usage: tcrtoken 2 ABCDEFGHIJKLMNOPQRSTUVWXYZ.abc.123456789";

	//	// A short description of this command
	//	public override string Description
	//		=> "Sets the BOT token for TerrariaChatRelay.";

	//	public override void Action(CommandCaller caller, string input, string[] args)
	//	{
	//		// Clear sensitive token from console
	//		Console.Clear();

	//		// Checking input Arguments
	//		if (args.Length == 0)
	//		{
	//			throw new UsageException("At least one argument was expected.");
	//		}
	//		if (args.Length >= 3)
	//		{
	//			throw new UsageException("Too many arguments were provided.");
	//		}

	//		var successMessage = "Bot token successfully added! Use 'tcrreload' to load new changes.";

	//		if (args.Length > 0)
	//		{
	//			string BotToken;
	//			if(args.Length == 2)
	//			{
	//				// The user supplies the index starting from 1 instead of 0
	//				int endpointIndexStartingFromOne;
	//				// Parsing endpoint index
	//				if (!int.TryParse(args[0], out endpointIndexStartingFromOne))
	//				{
	//					throw new UsageException(args[0] + " is not a correct endpoint position (must be valid integer value).");
	//				}

	//				if (DiscordPlugin.Config.EndPoints.Count < endpointIndexStartingFromOne)
	//				{
	//					throw new UsageException(args[0] + $" is not a valid endpoint (there are only {DiscordPlugin.Config.EndPoints.Count} endpoints!).");
	//				}

	//				// The inputs are ToLower'ed, so we must get the raw input.
	//				BotToken = input.Split(" ")[2];

	//				DiscordPlugin.Config.EndPoints[endpointIndexStartingFromOne - 1].BotToken = BotToken;

	//				DiscordPlugin.Config.SaveJson();
	//				Console.WriteLine($"Endpoint {endpointIndexStartingFromOne}: {successMessage}");
	//				return;
	//			}
	//			if (args.Length == 1 && DiscordPlugin.Config.EndPoints.Count >= 2) 
	//			{
	//				Console.WriteLine($"{DiscordPlugin.Config.EndPoints.Count} endpoints found!");
	//				Console.WriteLine();

	//				var i = 1;
	//				foreach (var Endpoint in DiscordPlugin.Config.EndPoints)
	//				{
	//					Console.WriteLine($"[Endpoint {i}]");
	//					if(Endpoint.Channel_IDs?.Count > 0)
	//					{
	//						foreach (var channelIdListing in Endpoint.Channel_IDs)
	//						{
	//							Console.WriteLine($"- Channel: {channelIdListing}");
	//						}
	//					}
	//					else
	//					{
	//						Console.WriteLine($"- No channels found");
	//					}
	//					i++;
	//				}
	//				Console.WriteLine();
	//				Console.WriteLine("To update the token, add the number of the endpoint you wish to update.");
	//				Console.WriteLine("- Example: /tcroken 1 TOKENHERE");
	//				return;
	//			}

	//			// The inputs are ToLower'ed, so we must get the raw input.
	//			BotToken = input.Split(" ")[1];
	//			if (args.Length == 1 && DiscordPlugin.Config.EndPoints.Count == 0)
	//			{

	//				DiscordPlugin.Config.EndPoints.Add(new Endpoint()
	//				{
	//					BotToken = BotToken
	//				});
	//				DiscordPlugin.Config.SaveJson();
	//				Console.WriteLine(successMessage);
	//				return;
	//			}
	//			if (args.Length == 1 && DiscordPlugin.Config.EndPoints.Count == 1)
	//			{
	//				DiscordPlugin.Config.EndPoints.First().BotToken = BotToken;
	//				DiscordPlugin.Config.SaveJson();
	//				Console.WriteLine(successMessage);
	//				return;
	//			}

	//			throw new UsageException("An unexpected error occurred using /tcrtoken.");
	//		}
	//	}
	//}
}
