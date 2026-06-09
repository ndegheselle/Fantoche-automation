namespace Automation.Shared.Data.Execution
{
    /// <summary>
    /// Identifier of a package (name and version)
    /// </summary>
    public class PackageIdentifier
    {
        public string Id { get; set; } = "";
        public Version Version { get; set; } = new Version();

        public override string ToString()
        {
            return $"'{Id}' (version {Version})";
        }
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
