using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace TCRCore.Helpers
{
    public abstract class SimpleConfig<T> : ISimpleConfig<T> where T : SimpleConfig<T>, new()
    {
        [JsonIgnore]
        public abstract string FileName { get; set; }

        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);

        public string ToJson(T config)
            => JsonConvert.SerializeObject(config, Formatting.Indented);

        public void SaveJson()
        {
            string file = Path.Combine(Terraria.Main.SavePath, FileName);
            File.WriteAllText(file, ToJson());
        }

        public void SaveJson(T config)
        {
            string file = Path.Combine(Terraria.Main.SavePath, FileName);
            File.WriteAllText(file, ToJson(config));
        }

        public virtual T GetOrCreateConfiguration()
        {
            string file = Path.Combine(Terraria.Main.SavePath, FileName);
            T config;

            if (!File.Exists(file))
                config = CreateConfigurationAt(file);
            else
                config = GetConfigurationsAt(file);
            SaveJson(config);

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
