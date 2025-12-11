namespace Automation.Shared.Data
{
    public class PackageIdentifier
    {
        public string Identifier { get; set; } = "";
        public Version Version { get; set; } = new Version();
    }

    public class PackageInfos
    {
        public PackageIdentifier Identifier { get; set; } = new PackageIdentifier();
        public string Description { get; set; } = "";
    }
}
