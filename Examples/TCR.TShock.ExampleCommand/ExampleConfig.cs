using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TerrariaChatRelay;
using TerrariaChatRelay.Helpers;

namespace TCRExampleCommand
{
	// Inherit from SimpleConfig if you want to use TCR's built-in configuration generator
	// It is optional. You can use alternative configuration libraries!
	public class ExampleConfig : SimpleConfig<ExampleConfig>
	{
		public override string FileName { get; set; }
			= Path.Combine(Global.ModConfigPath, "TCR.MoreTShockCommands.json");

		// Supplying defaults helps when creating a brand new configuration
		// These values are use when generating configs on the first run
		[JsonProperty(Order = 1)]
		public bool EnableExampleCommand { get; set; } = true;
		[JsonProperty(Order = 10)]
		public string ExampleString { get; set; } = "";
		[JsonProperty(Order = 20)]
		public bool ExampleBool { get; set; } = true;
		[JsonProperty(Order = 30)]
		public List<ulong> ExampleList { get; set; } = new List<ulong>();
		[JsonProperty(Order = 80)]
		public List<ExampleObject> ExampleObjectList { get; set; } = new List<ExampleObject>();
		[JsonProperty(Order = 3000)]
		public string ExampleHelp { get; set; } = "You can set this string to anything and it will appear in the config!";
		[JsonProperty(Order = 5000)]
		public Dictionary<string, string> ExampleDictionary { get; set; } = new Dictionary<string, string>
		{
			["ExampleKey"] = "ExampleValue"
		};


		/// <summary>
		/// All variables must ALWAYS be initialized inside of the classes. If no default values are provides, they will be NULL!
		/// </summary>
		public class ExampleObject
        {
			public string Example1 { get; set; } = "";
			public int Example2 { get; set; } = 0;
        }
		public ExampleConfig()
		{
			// Check if the file exists before inserting
			// Otherwise, you will always insert an empty object on every load!
			if (!File.Exists(FileName))
			{
				// When a config is newly generated,
				// it will start with one instance already put into the list
				ExampleObjectList.Add(new ExampleObject());
			}
		}
	}
}
