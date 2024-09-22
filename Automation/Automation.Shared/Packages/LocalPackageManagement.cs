using Automation.Shared.Base;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
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
            PackageSearchResource resource = _source.GetResource<PackageSearchResource>();
            var results = await resource.SearchAsync(
                name,
                new SearchFilter(includePrerelease: false),
                page - 1,
                pageSize,
                NullLogger.Instance,
                CancellationToken.None);
            return new ListPageWrapper<PackageInfos>()
            {
                Page = page,
                PageSize = pageSize,
                Data = results.Select(x => new PackageInfos(x)).ToList()
            };
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
            var latestPackage = searchMetadata
                .OrderByDescending(p => p.Identity.Version)
                .FirstOrDefault();

            if (latestPackage == null)
                return null;

            return new PackageInfos(latestPackage);
        }

        public Task<PackageInfos> GetInfosFromFileAsync(string filePath)
        {
            using var packageReader = new PackageArchiveReader(filePath);
            var nuspecReader = packageReader.NuspecReader;
            string id = nuspecReader.GetId();
            var version = nuspecReader.GetVersion();
            string name = nuspecReader.GetTitle();
            if (string.IsNullOrEmpty(name))
                name = id;

            return Task.FromResult(new PackageInfos
            {
                Id = id,
                Name = name,
                Description = nuspecReader.GetDescription(),
                Versions = new List<Version>()
                {
                    new Version(version)
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
