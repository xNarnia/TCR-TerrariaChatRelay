using System;
using System.IO;
using System.Linq;
using System.Net;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using TerrariaChatRelay.Helpers;
using TerrariaChatRelay.Clients.DiscordClient;
using System.Reflection;
using TerrariaChatRelay.TMLHooks;
using System.Collections.Generic;

namespace TerrariaChatRelay
{
    public class TerrariaChatRelay : Mod
    {
        public Version LatestVersion = new Version("0.0.0.0");
        public Mod SubworldLib;
        public static Mod TCRMod;
        public List<BaseHook> Hooks;

        public TerrariaChatRelay()
        {
            Hooks = new List<BaseHook>();
        }

        public override void Load()
        {
            base.Load();

            // Set security protocol to allow requests to GitHub for version checking
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Config handling
            var config = ModContent.GetInstance<Config>();
            string modConfigFilePath = Path.Combine(Main.SavePath, "ModConfigs", $"{Name}_Config.json");
            string discordConfigPath = Path.Combine(Main.SavePath, "ModConfigs", "TerrariaChatRelay", "TerrariaChatRelay-Discord.json");
            
            // Compare times to see which is newest. Use newest config.
            DateTime mConfigWriteTime = File.GetLastWriteTime(modConfigFilePath);
            DateTime dConfigWriteTime = File.GetLastWriteTime(discordConfigPath);

            if (mConfigWriteTime > dConfigWriteTime)
            {
                FileInfo file = new FileInfo(discordConfigPath);
                file.Directory.Create();
                File.WriteAllText(discordConfigPath, JsonConvert.SerializeObject(config));
                new DiscordConfig().GetOrCreateConfiguration().SaveJson(); // Used to propogate comments through file
            }
            else
            {
                FileInfo file = new FileInfo(discordConfigPath);
                file.Directory.Create();
                File.WriteAllText(modConfigFilePath, new DiscordConfig().GetOrCreateConfiguration().ToJson());
                new DiscordConfig().GetOrCreateConfiguration().SaveJson(); // Used to propogate comments through file
            }

            Global.Config = new TCRConfig().GetOrCreateConfiguration();

            // Hooks - Located in ./TMLHooks/
            var hookTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(TCRHook), false).Any());
            
            foreach (var hookType in hookTypes)
            {
                var hook = Activator.CreateInstance(hookType, this) as BaseHook;
                hook.Attach();
                Hooks.Add(hook);
            }

            // Add subscribers to list
            var adapter = new Adapter();
            adapter.Version = Version;
            Core.Initialize(adapter);
            Core.ConnectClients();

            if (Global.Config.CheckForLatestVersion)
                Task.Run(GetLatestVersionNumber);
        }

        public override void PostSetupContent()
        {
            base.PostSetupContent();

            // Mod References
            ModLoader.TryGetMod("SubworldLibrary", out SubworldLib);
        }

        /// <summary>
        /// <para>Loads the build.txt from GitHub to check if there is a newer version of TCR available. </para>
        /// If there is, a message will be displayed on the console and prepare a message for sending when the world is loading.
        /// </summary>
        public async Task GetLatestVersionNumber()
        {
            var client = new HttpClient();
            LatestVersion = this.Version;

            HttpResponseMessage res;
            try
            {
                res = await client.GetAsync("https://raw.githubusercontent.com/xNarnia/TCR-TerrariaChatRelay/master/version.txt");
            }
            catch (Exception)
            {
                return;
            }

            using (StreamReader sr = new StreamReader(res.Content.ReadAsStream()))
            {
                string buildtxt = sr.ReadToEnd();
                buildtxt = buildtxt.ToLower();

                string line = "";
                using (StringReader stringreader = new StringReader(buildtxt))
                {
                    do
                    {
                        line = stringreader.ReadLine();
                        if (line.Contains("version"))
                        {
                            line = line.Replace(" ", "");
                            line = line.Replace("version=", "");

                            LatestVersion = new Version(line);
                            if (LatestVersion > Version)
                                PrettyPrint.Log("Adapter", $"A new version of TCR is available: V.{LatestVersion.ToString()}");

                            line = null;
                        }
                    }
                    while (line != null);
                }
            }
        }

        /// <summary>
        /// Handle disconnect for all clients, remove events, and finally dispose of config.
        /// </summary>
        public override void Unload()
        {
            Core.DisconnectClients();
            Hooks.ForEach(x => x.Detach());
            Global.Config = null;
        }
    }
    public static class Extensions
    {
        public static TCRPlayer ToTCRPlayer(this Player player, int id)
        {
            return new TCRPlayer()
            {
                PlayerId = id,
                Name = player.name
            };
        }
    }
}