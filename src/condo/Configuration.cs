namespace condo
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    sealed partial class Configuration : IDisposable
    {
        private const string AppDataFolder = "condo";
        private const string SettingsFilename = "settings.json";
        private FileSystemWatcher fileWatcher;

        /// <summary>
        /// Raised when the underlying configuration file changes (even if the contents did not).
        /// </summary>
        public EventHandler Changed;

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
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var stream = new StreamReader(fs))
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
            config.StartWatch();
            return config;
        }

        public static string GetDefaultFilename()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolder, SettingsFilename);
        }

        public void ShellOpen()
        {
            System.Diagnostics.Process.Start(this.Filename);
        }

        private void Save(string filename)
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

        private void StartWatch()
        {
            this.fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(this.Filename), Path.GetFileName(this.Filename));
            this.fileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size;
            this.fileWatcher.Changed += this.OnFileChanged;
            this.fileWatcher.Created += this.OnFileChanged;
            this.fileWatcher.EnableRaisingEvents = true;
        }

        private void OnFileChanged(object sender, EventArgs args)
        {
            this.Changed?.Invoke(this, null);
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.fileWatcher != null)
                    {
                        this.fileWatcher.Dispose();
                        this.fileWatcher = null;
                    }
                }

                this.disposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}
