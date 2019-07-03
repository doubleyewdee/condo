namespace condo
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    sealed class Configuration
    {
        private const string AppDataFolder = "condo";
        private const string SettingsFilename = "settings.json";

        [JsonProperty]
        public string FontFamily { get; set; } = "Consolas";

        [JsonProperty]
        public int FontSize { get; set; } = 12;

        public string Filename { get; private set; }

        // We'll leak any exceptions related to invalid json.
        public static Configuration Load(string filename)
        {
            Configuration config;
            try
            {
                using (var stream = File.OpenText(filename))
                {
                    ConsoleBuffer.Logger.Verbose($"Writing configuration to {filename}");
                    var reader = new JsonSerializer();
                    config = (Configuration)reader.Deserialize(stream, typeof(Configuration));
                }
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                ConsoleBuffer.Logger.Verbose($"Configuration in {filename} not found, creating...");
                config = new Configuration();
                config.Save(filename);
            }

            config.Filename = filename;
            return config;
        }

        public static string GetDefaultFilename()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolder, SettingsFilename);
        }

        public void Save(string filename)
        {
            var directoryName = Path.GetDirectoryName(filename);
            if (!Directory.Exists(directoryName))
            {
                ConsoleBuffer.Logger.Verbose($"Creating directory {directoryName} to save config.");
                Directory.CreateDirectory(directoryName);
            }

            using (var stream = File.CreateText(filename))
            {
                var writer = new JsonSerializer();
                writer.Formatting = Formatting.Indented;
                writer.Serialize(stream, this);
            }
        }

        public void ShellOpen()
        {
            System.Diagnostics.Process.Start(this.Filename);
        }
    }
}
