using Discord;
using Discord.WebSocket;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.DiscordClient.Services.SlashCommands;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.Clients.DiscordClient.Services
{
	/// <summary>
	/// Handles loading, registering, and interactions of TCR Discord SlashCommands.
	/// </summary>
	public class SlashCommandService : IDiscordService
	{
		public Dictionary<string, ISlashCommand> Commands { get; set; }
		private DiscordSocketClient socket { get; set; }
		private Regex regex { get; set; }

		public SlashCommandService(DiscordSocketClient parentSocket)
		{
			socket = parentSocket;
			socket.SlashCommandExecuted += SlashCommandExecuted;
			regex = new Regex(@"^[\w-]{3,32}$");
			LoadCommands();
		}

		public void Start()
		{
			Task.Run(async () => RegisterGuildCommandsAsync(socket));
		}

		public void Stop()
		{
			socket.SlashCommandExecuted -= SlashCommandExecuted;
		}

		public void Dispose()
		{
			Commands = null;
			socket = null;
		}

		/// <summary>
		/// Handles slash commands executed from client.
		/// </summary>
		/// <param name="command">The command executed from the client.</param>
		private async Task SlashCommandExecuted(SocketSlashCommand command)
		{
			var tcrCommand = Commands[command.CommandName];
			var errorMessage = "";

			try
			{
				await tcrCommand.Run(command);
			}
			catch (Exception e)
			{
				errorMessage = e.Message;
			}

			if (errorMessage != "" && errorMessage != null)
			{
				var embed = new EmbedBuilder()
					.WithDescription(errorMessage)
					.WithColor(Color.Red)
					.Build();
				try
				{
					await command.RespondAsync(null, [embed], false, tcrCommand.Ephemeral);
				}
				catch (Exception ex)
				{
					PrettyPrint.Log("Discord", "An error occurred attempting to relay an error from a command. Reason: " + ex.Message + "\nCommand [" + tcrCommand.Name + "] error message: " + errorMessage);
				}
			}
		}

		/// <summary>
		/// Load TCR SlashCommands from assembly.
		/// </summary>
		public void LoadCommands()
		{
			Commands = new Dictionary<string, ISlashCommand>();
			var assembly = Assembly.GetExecutingAssembly();

			foreach (var type in assembly.GetTypes())
			{
				if (typeof(ISlashCommand).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
				{
					var command = (ISlashCommand)Activator.CreateInstance(type);
					if (!Commands.ContainsKey(command.Name))
					{
						if (regex.IsMatch(command.Name))
							Commands.Add(command.Name.ToLower(), command);
						else
							PrettyPrint.Log("Discord", $"Unable to add command [{command.Name}]: Illegal characters.", ConsoleColor.Red);
					}
					else
					{
						PrettyPrint.Log("Discord", $"Could not add SlashCommand [{command.Name}] because it already exists!", ConsoleColor.Red);
					}
				}
			}

			PrettyPrint.Log("Discord", "Slash commands loaded.");
		}

		/// <summary>
		/// Register SlashCommands with global context.
		/// </summary>
		/// <param name="client">The endpoint to subscribe the commands with.</param>
		public async Task RegisterGlobalCommandsAsync(DiscordSocketClient client)
		{

		}

		/// <summary>
		/// Register SlashCommands with all guilds with guild context.
		/// </summary>
		/// <param name="client">The endpoint to subscribe the commands with.</param>
		public async Task RegisterGuildCommandsAsync(DiscordSocketClient client)
		{
			foreach (var guild in client.Guilds)
			{
				foreach (var commandEntry in Commands)
				{
					var key = commandEntry.Key;
					var command = commandEntry.Value;

					SlashCommandBuilder builder = new SlashCommandBuilder();
					builder
						.WithName(key)
						.WithDescription(command.Description)
						.WithDefaultMemberPermissions(command.DefaultPermission);

					builder = command.Builder(builder);
					try
					{
						await client.Rest.CreateGuildCommand(builder.Build(), guild.Id);
						await Task.Delay(100);
					}
					catch (Exception ex)
					{
						PrettyPrint.Log("Discord", "An error occurred registering guild commands. Reason: " + ex.Message);
						return;
					}
				}
			}
		}
	}
}
