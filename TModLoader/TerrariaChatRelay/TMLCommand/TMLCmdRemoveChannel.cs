using System;
using System.Linq;
using Terraria.ModLoader;
using TerrariaChatRelay.Clients.DiscordClient;

namespace TerrariaChatRelay.TMLCommand
{
	public class TMLCmdRemoveChannel : ModCommand
	{
		public override CommandType Type
			=> CommandType.Console;

		// The desired text to trigger this command
		public override string Command
			=> "tcrremovechannel";

		// A short usage explanation for this command
		public override string Usage
			=> "tcrremovechannel channelid - Removes the channel id" +
			"tcrremovechannel channelid endpointPosition - Removes the channel id to the specified endpoint" +
			"   Example Usage: tcrremovechannel 1234567890" +
			"   Example Usage: tcraddchannel 2 1234567890";

		// A short description of this command
		public override string Description
			=> "Removes a channel from TerrariaChatRelay.";

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

				// Can't remove from what doesn't exist
				if (DiscordPlugin.Config.EndPoints.Count == 0)
				{
					Console.WriteLine("There are no endpoints to remove channel id's from.");
					return;
				}

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

				var successMessage = "Channel ID successfully removed! Use 'tcrreload' to load new changes.";
				var failMessage = "No channel found by that id.";

				if (args.Length == 2)
				{
					if (DiscordPlugin.Config.EndPoints.Count < endpointIndexStartingFromOne)
					{
						throw new UsageException(args[0] + $" is not a valid endpoint (there are only {DiscordPlugin.Config.EndPoints.Count} endpoints!).");
					}

					var endpoint = DiscordPlugin.Config.EndPoints[endpointIndexStartingFromOne - 1];
					if (endpoint.Channel_IDs?.Contains(channelId) == true)
					{
						endpoint.Channel_IDs.RemoveAll(x => x == channelId);
						DiscordPlugin.Config.SaveJson();
						Console.WriteLine($"Endpoint {endpointIndexStartingFromOne}: {successMessage}");
					}
					else
					{
						Console.WriteLine($"Endpoint {endpointIndexStartingFromOne}: {failMessage}");
					}
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
					Console.WriteLine("- Example: /tcrremovechannel 1 CHANNELIDHERE");
					return;
				}
				if (args.Length == 1 && DiscordPlugin.Config.EndPoints.Count == 1)
				{
					var endpoint = DiscordPlugin.Config.EndPoints.First();
					if (endpoint.Channel_IDs.Contains(channelId))
					{
						endpoint.Channel_IDs.RemoveAll(x => x == channelId);
						DiscordPlugin.Config.SaveJson();
						Console.WriteLine(successMessage);
					}
					else
					{
						Console.WriteLine(failMessage);
					}
					return;
				}

				throw new UsageException("An unexpected error occurred using /tcrremovechannel.");
			}
		}
	}
}
