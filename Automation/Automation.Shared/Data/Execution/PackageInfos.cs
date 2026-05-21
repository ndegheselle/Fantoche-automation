namespace Automation.Shared.Data.Execution
{
    /// <summary>
    /// Identifier of a package (name and version)
    /// </summary>
    public class PackageIdentifier
    {
        public string Identifier { get; set; } = "";
        public Version Version { get; set; } = new Version();
    }

    /// <summary>
    /// Information about a package
    /// </summary>
    public class PackageInfos
    {
        public PackageIdentifier Identifier { get; set; } = new PackageIdentifier();
        public string Description { get; set; } = "";
    }
}
