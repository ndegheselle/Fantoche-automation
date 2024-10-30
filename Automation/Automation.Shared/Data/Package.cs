namespace Automation.Shared.Data
{
    public class PackageInfos
    {
        public string Id { get; set; } = "";
        public string Description { get; set; } = "";
        public Version Version { get; set; } = new Version();
    }

    public class TargetedPackage
    {
        public string Id { get; set; } = "";
        public Version Version { get; set; } = new Version();
        public PackageClass Class { get; set; } = new PackageClass();
    }

    public class PackageClass
    {
        public string Dll { get;set; }
        public string Name { get; set; }

        public PackageClass()
        {
            Dll = string.Empty;
            Name = string.Empty;
        }

        public PackageClass(string dll, string name)
        {
            Dll = dll;
            Name = name;
        }
    }
}