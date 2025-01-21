using System;
using System.IO;
using Newtonsoft.Json;

namespace TerrariaChatRelay.Helpers
{
	public abstract class SimpleConfig<T> : ISimpleConfig<T> where T : SimpleConfig<T>, new()
	{
		[JsonIgnore]
		public abstract string FileName { get; set; }

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}

		public void SaveJson()
		{
			string file = Path.Combine(Terraria.Main.SavePath, FileName);
			File.WriteAllText(file, this.ToJson());
		}

		public virtual T GetOrCreateConfiguration()
		{
			string file = Path.Combine(Terraria.Main.SavePath, FileName);
			T config;

			if (!File.Exists(file))
				config = CreateConfigurationAt(file);
			else
			{
				var rawConfig = File.ReadAllText(FileName);
				config = JsonConvert.DeserializeObject<T>(rawConfig);
				var deserializedConfig = config.ToJson();

				// If entries are missing or the config isn't formatted, fix it
				if (rawConfig != deserializedConfig)
				{
					File.WriteAllText(FileName, deserializedConfig);
				}
			}

			return config;
		}

		public bool Exists()
		{
			string file = Path.Combine(Terraria.Main.SavePath, FileName);
			return File.Exists(file);
		}

		public static T LoadConfigFile()
		{
			return new T().GetOrCreateConfiguration() as T;
		}

		private T CreateConfigurationAt(string file)
		{
			string path = Path.GetDirectoryName(file);
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			SaveJson();
			return this as T;
		}
	}

	public interface ISimpleConfig<T>
	{
		// Variables
		string FileName { get; set; }

		// Methods
		string ToJson();
		void SaveJson();
		bool Exists();
		T GetOrCreateConfiguration();
	}
}
