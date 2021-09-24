using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Command
{
	public class CommandPayload : ICommandPayload
	{
		private ICommandService CommandServ { get; }
		public ICommand Command { get; }
		public string Parameters { get; }
		public TCRClientUser UserExecutor { get; }
		public bool Executed { get; }

		public CommandPayload(ICommandService commandServ, ICommand command, string parameters, TCRClientUser userExecutor)
		{
			CommandServ = commandServ;
			Command = command;
			Parameters = parameters;
			UserExecutor = userExecutor;
			Executed = false;
		}

		public string Execute()
		{
			return CommandServ.ExecuteCommand(this);
		}
	}
}
