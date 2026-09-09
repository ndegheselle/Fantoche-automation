using System.IO;

namespace Automation.App
{
    public class Settings
    {
        public const string ApplicationName = "Automation";

        public string PackagesFolderPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "nuggets");

        public string LocalFolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
            ApplicationName);
    }
}
