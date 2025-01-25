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
using System.Net;

namespace TerrariaChatRelay.TMLCommand
{
	public class TMLCmdAddChannel : ModCommand
	{
		public override CommandType Type
			=> CommandType.Console;

		// The desired text to trigger this command
		public override string Command
			=> "tcraddchannel";

		// A short usage explanation for this command
		public override string Usage
			=> "tcraddchannel channelid - Adds the channel id" +
			"tcraddchannel channelid endpointPosition - Adds the channel id to the specified endpoint" +
			"   Example Usage: tcraddchannel 1234567890" +
			"   Example Usage: tcraddchannel 2 1234567890";

		// A short description of this command
		public override string Description
			=> "Adds a channel to TerrariaChatRelay.";

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			// Checking input Arguments
			if (args.Length == 0)
			{
				throw new UsageException("At least one argument was expected.");
			}
			if (args.Length >= 3)
			{
				throw new UsageException("Too many arguments were provided.");
			}

			if (args.Length > 0)
			{
				// The user supplies the index starting from 1 instead of 0
				int endpointIndexStartingFromOne = 0;
				ulong channelId = 0;

				if (args.Length == 1)
				{
					if (!ulong.TryParse(args[0], out channelId))
						throw new UsageException(args[0] + " is not a correct channel id (must be numbers only!).");
				}
				if (args.Length == 2)
				{
					if (!int.TryParse(args[0], out endpointIndexStartingFromOne))
						throw new UsageException(args[0] + " is not a correct endpoint position (must be numbers only!).");

					if (!ulong.TryParse(args[1], out channelId))
						throw new UsageException(args[1] + " is not a correct channel id (must be numbers only!).");
				}

				var successMessage = "Channel ID successfully added! Use 'tcrreload' to load new changes.";

				if (args.Length == 2)
				{
					if (DiscordPlugin.Config.EndPoints.Count < endpointIndexStartingFromOne)
					{
						throw new UsageException(args[0] + $" is not a valid endpoint (there are only {DiscordPlugin.Config.EndPoints.Count} endpoints!).");
					}

					var endpoint = DiscordPlugin.Config.EndPoints[endpointIndexStartingFromOne - 1];
					endpoint.Channel_IDs.RemoveAll(x => x == 0);
					endpoint.Channel_IDs.Add(channelId);
					DiscordPlugin.Config.SaveJson();
					Console.WriteLine($"Endpoint {endpointIndexStartingFromOne}: {successMessage}");
					return;
				}
				if (args.Length == 1 && DiscordPlugin.Config.EndPoints.Count >= 2)
				{
					Console.WriteLine($"{DiscordPlugin.Config.EndPoints.Count} endpoints found!");
					Console.WriteLine();

					var i = 1;
					foreach (var Endpoint in DiscordPlugin.Config.EndPoints)
					{
						Console.WriteLine($"[Endpoint {i}]");
						if (Endpoint.Channel_IDs?.Count > 0)
						{
							foreach (var channelIdListing in Endpoint.Channel_IDs)
							{
								Console.WriteLine($"- Channel: {channelIdListing}");
							}
						}
						else
						{
							Console.WriteLine($"- No channels found");
						}
						i++;
					}
					Console.WriteLine();
					Console.WriteLine("To update the channel ids, add the number of the endpoint you wish to update.");
					Console.WriteLine("- Example: /tcraddchannel 1 CHANNELIDHERE");
					return;
				}
				if (args.Length == 1 && DiscordPlugin.Config.EndPoints.Count == 0)
				{
					DiscordPlugin.Config.EndPoints.Add(new Endpoint()
					{
						Channel_IDs = { channelId }
					});
					DiscordPlugin.Config.SaveJson();
					Console.WriteLine(successMessage);
					return;
				}
				if (args.Length == 1 && DiscordPlugin.Config.EndPoints.Count == 1)
				{
					var endpoint = DiscordPlugin.Config.EndPoints.First();
					endpoint.Channel_IDs.RemoveAll(x => x == 0);
					endpoint.Channel_IDs.Add(channelId);
					DiscordPlugin.Config.SaveJson();
					Console.WriteLine(successMessage);
					return;
				}

				throw new UsageException("An unexpected error occurred using /tcraddchannel.");
			}
		}
	}
}
