using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Automation.Shared.Packages
{
    public struct PackageInfos
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<PackageVersion> Versions { get; set; } = [];

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
        public PackageVersion Version { get; set; }
    }

    public struct PackageVersion
    {
        public uint Major { get; set; }
        public uint Minor { get; set; }
        public uint Patch { get; set; }

        public PackageVersion(uint major = 0, uint minor = 0, uint patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public PackageVersion(NuGetVersion version)
        {
            Major = (uint)version.Major;
            Minor = (uint)version.Minor;
            Patch = (uint)version.Patch;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }
}
