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
			return JsonConvert.SerializeObject(this);
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
                config = GetConfigurationsAt(file);

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

        private T GetConfigurationsAt(string file)
		{
			var config = JsonConvert.DeserializeObject<T>(File.ReadAllText(file));
            return config;
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
