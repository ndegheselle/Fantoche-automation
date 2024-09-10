using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Automation.Shared.Base
{
    public class Package
    {
        public Package(IPackageSearchMetadata metadata)
        {
            Id = metadata.Identity.Id;
            Version = metadata.Identity.Version.ToString();
            Name = metadata.Title;
            Description = metadata.Description;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
    }

    public class PackageManagement
    {
        private readonly SourceRepository _source;

        public PackageManagement(string location)
        {
            List<Lazy<INuGetResourceProvider>> providers =
            [
                .. Repository.Provider.GetCoreV3(),  // Add v3 API support
            ];
            PackageSource packageSource = new PackageSource(location);
            _source = new SourceRepository(packageSource, providers);
        }

        public async Task<IEnumerable<Package>> SearchAsync(string name = "", int page = 0, int pageSize = 50)
        {
            PackageSearchResource resource = await _source.GetResourceAsync<PackageSearchResource>();
            var results = await resource.SearchAsync(
                name,
                new SearchFilter(includePrerelease: false),
                page,
                pageSize,
                NullLogger.Instance,
                CancellationToken.None);
            return results.Select(x => new Package(x));
        }
    }
}
