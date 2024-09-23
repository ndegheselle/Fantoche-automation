using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Automation.Shared.Packages
{
    public struct PackageInfos
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Version> Versions { get; set; }

        public PackageInfos() { }

        public PackageInfos(IPackageSearchMetadata metadata)
        {
            Id = metadata.Identity.Id;
            Name = metadata.Title;
            Description = metadata.Description;
            Versions =  new List<Version>()
            {
                new Version(metadata.Identity.Version)
            };
        }
    }
    public struct TargetedPackage
    {
        public string Id { get; set; }
        public Version Version { get; set; }
    }

    public struct Version
    {
        public uint Major { get; set; }
        public uint Minor { get; set; }
        public uint Patch { get; set; }

        public Version(uint major = 0, uint minor = 0, uint patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public Version(NuGetVersion version)
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
