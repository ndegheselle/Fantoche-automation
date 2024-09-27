using Automation.Shared.Base;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System.Data;

namespace Automation.Shared.Packages
{
    public class LocalPackageManagement : IPackageManagement
    {
        private const string PACKAGE_EXTENSION = ".nupkg";
        private readonly string _folder;
        private readonly SourceRepository _source;

        public LocalPackageManagement(string folder)
        {
            _folder = folder;
            List<Lazy<INuGetResourceProvider>> providers =
            [
                .. Repository.Provider.GetCoreV3(),  // Add v3 API support
            ];
            PackageSource packageSource = new PackageSource(folder);
            _source = new SourceRepository(packageSource, providers);
        }

        public async Task<ListPageWrapper<PackageInfos>> SearchAsync(string name = "", int page = 1, int pageSize = 50)
        {
            // XXX : difference with LocalPackageSearchResource ?
            PackageSearchResource resource = _source.GetResource<PackageSearchResource>();
            var results = await resource.SearchAsync(
                name,
                new SearchFilter(includePrerelease: false),
                page - 1,
                pageSize,
                NullLogger.Instance,
                CancellationToken.None);
            var searchResult = new ListPageWrapper<PackageInfos>()
            {
                Page = page,
                PageSize = pageSize
            };

            foreach (var result in results)
            {
                var versions = await result.GetVersionsAsync();
                var package = new PackageInfos(result);
                package.Versions = versions.Select(v => new PackageVersion(v.Version)).ToList();
                searchResult.Data.Add(package);
            }

            return searchResult;
        }

        public async Task<PackageInfos?> GetInfosFromIdAsync(string packageId)
        {
            var packageMetadataResource = await _source.GetResourceAsync<PackageMetadataResource>();

            var searchMetadata = await packageMetadataResource.GetMetadataAsync(
                packageId,
                includePrerelease: false,
                includeUnlisted: false,
                null,
                NullLogger.Instance,
                CancellationToken.None);

            // TODO : get a list of versions in package
            searchMetadata = searchMetadata.OrderByDescending(p => p.Identity.Version);

            if (searchMetadata.FirstOrDefault() == null)
                return null;

            var package = new PackageInfos(searchMetadata.First());
            package.Versions = searchMetadata.Select(x => new PackageVersion(x.Identity.Version)).ToList();
            return package;
        }

        public Task<PackageInfos> GetInfosFromFileAsync(string filePath)
        {
            using var packageReader = new PackageArchiveReader(filePath);
            var nuspecReader = packageReader.NuspecReader;
            string id = nuspecReader.GetId();
            var version = nuspecReader.GetVersion();

            return Task.FromResult(new PackageInfos
            {
                Id = id,
                Description = nuspecReader.GetDescription(),
                Versions = new List<PackageVersion>()
                {
                    new PackageVersion(version)
                }
            });
        }

        public async Task<PackageInfos> CreatePackageFromFileAsync(string sourcePath, PackageInfos? package = null)
        {
            package ??= await GetInfosFromFileAsync(sourcePath);
            string fileName = $"{package.Value.Id}.{package.Value.Versions.Last()}{PACKAGE_EXTENSION}";

            string destinationPath = Path.Combine(_folder, fileName.ToLower());

            // Prevent copying if the file already exist
            if (!File.Exists(destinationPath))
                File.Copy(sourcePath, destinationPath);

            return package.Value;
        }

        public bool IsFileValidPackage(string filePath, out List<string> errors)
        {
            // TODO : check if valid nuget package
            // TODO : check if implement proper package class startpoint
            errors = new List<string>();
            return true;
        }
    }
}
