using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Automation.Shared.Packages
{
    public struct PackageInfos
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<Version> Versions { get; set; } = [];

        public PackageInfos() { }

        public PackageInfos(IPackageSearchMetadata metadata)
        {
            Id = metadata.Identity.Id;
            Description = metadata.Description;
        }
    }

    public struct TargetedPackage
    {
        public string Id { get; set; }
        public Version Version { get; set; }
    }
}
