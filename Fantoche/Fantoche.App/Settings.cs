using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Joufflu.Themes;

namespace Fantoche.App
{
    /// <summary>
    /// Settings of the application, kept in "settings.json" of its local folder. The mode and the
    /// supervisor are read once, when the application starts : the services are built from them, so
    /// changing them takes a restart.
    /// <para>
    /// The local folder also holds the data of the worker of the application ("worker") and, in
    /// standalone mode, of its supervisor ("supervisor").
    /// </para>
    /// </summary>
    public class Settings
    {
        public const string ApplicationName = "Automation";

        private static readonly JsonSerializerOptions FileOptions = new() { WriteIndented = true };

        [JsonIgnore]
        public string LocalFolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
            ApplicationName);

        public string Theme { get; set; } = ThemeManager.System;

        /// <summary>
        /// The supervisor is run in the application rather than reached through its API, the
        /// application being its only worker. Only for debugging or very particular uses.
        /// </summary>
        public bool IsStandalone { get; set; }

        public string SupervisorUrl { get; set; } = "http://localhost:8080/";

        /// <summary>
        /// <see cref="SupervisorUrl"/> as the base of the addresses of the supervisor.
        /// </summary>
        [JsonIgnore]
        public Uri SupervisorAddress => new(SupervisorUrl.EndsWith('/') ? SupervisorUrl : $"{SupervisorUrl}/");

        private string FilePath => Path.Combine(LocalFolderPath, "settings.json");

        /// <summary>
        /// The saved settings, the defaults when there are none or they can't be read.
        /// </summary>
        public static Settings Load()
        {
            Settings defaults = new();
            if (!File.Exists(defaults.FilePath))
                return defaults;

            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(defaults.FilePath)) ?? defaults;
            }
            catch (JsonException)
            {
                return defaults;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(LocalFolderPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, FileOptions));
        }
    }
}
