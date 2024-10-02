using Automation.Shared.Base;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Versioning;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

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
            var searchResult = new ListPageWrapper<PackageInfos>() { Page = page, PageSize = pageSize };

            foreach (var result in results)
            {
                var versions = await result.GetVersionsAsync();
                var package = new PackageInfos(result);
                package.Versions = versions.Select(
                    version => new Version(version.Version.Major, version.Version.Minor, version.Version.Patch))
                    .ToList();
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

            searchMetadata = searchMetadata.OrderByDescending(p => p.Identity.Version);

            if (searchMetadata.FirstOrDefault() == null)
                return null;

            var package = new PackageInfos(searchMetadata.First());
            package.Versions = searchMetadata.Select(
                x => new Version(x.Identity.Version.Major, x.Identity.Version.Minor, x.Identity.Version.Patch))
                .ToList();
            return package;
        }

        public Task<PackageInfos> GetInfosFromFileAsync(string filePath)
        {
            using var packageReader = new PackageArchiveReader(filePath);
            var nuspecReader = packageReader.NuspecReader;
            string id = nuspecReader.GetId();
            var version = nuspecReader.GetVersion();

            return Task.FromResult(
                new PackageInfos
                {
                    Id = id,
                    Description = nuspecReader.GetDescription(),
                    Versions = new List<Version>() { new Version(version.Major, version.Minor, version.Patch) }
                });
        }

        public async Task<PackageInfos> CreatePackageFromFileAsync(string sourcePath, PackageInfos? package = null)
        {
            package ??= await GetInfosFromFileAsync(sourcePath);
            string fileName = $"{package.Id}.{package.Versions.Last()}{PACKAGE_EXTENSION}";

            string destinationPath = Path.Combine(_folder, fileName.ToLower());

            // Prevent copying if the file already exist
            if (!File.Exists(destinationPath))
                File.Copy(sourcePath, destinationPath);

            return package;
        }

        public void RemoveFromIdAndVersion(string id, string version)
        {
            string fileName = $"{id}.{version}{PACKAGE_EXTENSION}";
            string destinationPath = Path.Combine(_folder, fileName.ToLower());
            // Prevent copying if the file already exist
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);
        }

        public bool IsFileValidPackage(string filePath, out List<string> errors)
        {
            // TODO : check if valid nuget package
            // TODO : check if implement proper package class startpoint
            errors = new List<string>();
            return true;
        }

        public async Task<IEnumerable<PackageClass>> GetPackageClasses(string id, string version)
        {
            var cache = new SourceCacheContext();
            var resource = await _source.GetResourceAsync<FindPackageByIdResource>();
            var packageVersion = NuGetVersion.Parse(version);
            using var packageStream = new MemoryStream();

            await resource.CopyNupkgToStreamAsync(
                id,
                packageVersion,
                packageStream,
                cache,
                NullLogger.Instance,
                CancellationToken.None);

            packageStream.Position = 0;

            // Read the package
            using var packageReader = new PackageArchiveReader(packageStream);
            var libItems = packageReader.GetLibItems().ToList();

            // Find compatible framework
            var compatibleItem = libItems.FirstOrDefault(x =>
                x.TargetFramework.Framework.Equals(".NETStandard") ||
                x.TargetFramework.Framework.Equals(".NETCoreApp"));

            if (compatibleItem == null)
            {
                throw new Exception("No compatible framework found in package");
            }

            // Load assemblies and find plugin types
            foreach (var file in compatibleItem.Items.Where(i => i.EndsWith(".dll")))
            {
                using var stream = packageReader.GetStream(file);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                var assembly = Assembly.Load(ms.ToArray());

                var types = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                pluginTypes.AddRange(types);
            }

            return pluginTypes;
        }
    }
}
