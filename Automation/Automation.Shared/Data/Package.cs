namespace Automation.Shared.Data
{
    public class PackageIdentifier
    {
        public string Id { get; set; } = "";
        public Version Version { get; set; } = new Version();
    }

    public class PackageInfos
    {
        public PackageIdentifier Identifier { get; set; } =  new PackageIdentifier();
        public string Description { get; set; } = "";
    }

    public class PackageClass
    {
        public string Dll { get; set; }
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

    public class TargetedPackage
    {
        public PackageIdentifier Identifier { get; set; }
        public PackageClass Class { get; set; }

        public TargetedPackage(PackageIdentifier identifier, PackageClass targetClass)
        {
            Identifier = identifier;
            Class = targetClass;
        }
    }
}