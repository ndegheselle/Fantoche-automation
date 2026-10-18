using System.IO;

namespace Fantoche.App
{
    public class Settings
    {
        public const string ApplicationName = "Automation";

        /// <summary>
        /// Supervisor the tasks set to run on a worker are sent to, none when not set.
        /// </summary>
        public Uri? SupervisorUrl { get; } = Environment.GetEnvironmentVariable("FANTOCHE_SUPERVISOR_URL") is string url ? new Uri(url) : null;

        public string PackagesFolderPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "nuggets");

        public string LocalFolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
            ApplicationName);
    }
}
