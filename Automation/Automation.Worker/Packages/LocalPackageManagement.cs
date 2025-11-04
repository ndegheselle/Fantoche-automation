using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Base;
using Automation.Shared.Data;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Reflection;
using System.Runtime.Versioning;

namespace Automation.Worker.Packages
{
    public class LocalPackageManagement : IPackageManagement
    {
        private readonly string _folder;
        private readonly SourceRepository _repository;
        private readonly SourceCacheContext _cacheContext;
        private readonly ILogger _logger;

        public LocalPackageManagement(string folder)
        {
            _folder = folder;
            var packageSource = new PackageSource(folder);
            _repository = Repository.Factory.GetCoreV3(packageSource);
            _cacheContext = new SourceCacheContext();
            _logger = NullLogger.Instance;
        }

        public async Task<ListPageWrapper<PackageInfos>> SearchAsync(string name = "", int page = 1, int pageSize = 50)
        {
            // XXX : difference with LocalPackageSearchResource ?
            PackageSearchResource resource = _repository.GetResource<PackageSearchResource>();
            var packages = await resource.SearchAsync(
                name,
                new SearchFilter(includePrerelease: false),
                page - 1,
                pageSize,
                NullLogger.Instance,
                CancellationToken.None);
            return new ListPageWrapper<PackageInfos>() { 
                Page = page, 
                PageSize = pageSize, 
                Total = -1,
                Data = packages.Select(x => x.ToPackageInfos()).ToList()
            };
        }

        public async Task<PackageInfos> GetInfosAsync(string packageId, Version? version)
        {
            var resource = await _repository.GetResourceAsync<PackageMetadataResource>();
            IPackageSearchMetadata metadata;
            if (version != null)
            {
                metadata = await resource.GetMetadataAsync(new PackageIdentity(packageId, new NuGetVersion(version)), _cacheContext, _logger, CancellationToken.None);
            }
            else
            {
                IEnumerable<IPackageSearchMetadata> versions = await resource.GetMetadataAsync(packageId, true, false, _cacheContext, _logger, CancellationToken.None);

                var latestVersion = versions.MaxBy(x => x.Identity.Version) ?? throw new Exception($"No package found for the id {packageId}");
                metadata = latestVersion;
            }

            return metadata.ToPackageInfos();
        }

        public PackageInfos GetInfosFromStream(Stream stream)
        {
            using var packageReader = new PackageArchiveReader(stream);
            return packageReader.NuspecReader.ToPackageInfos();
        }

        public async Task<IEnumerable<Version>> GetVersionsAsync(string packageId)
        {
            FindPackageByIdResource resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            // Get all versions of the package
            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                packageId,
                _cacheContext,
                _logger,
                CancellationToken.None);
            return versions.Select(x => x.Version).Reverse();
        }

        public async Task<PackageInfos> CreateFromStreamAsync(Stream stream)
        {
            var packageReader = new PackageArchiveReader(stream);
            var identity = await packageReader.GetIdentityAsync(CancellationToken.None);

            string packagePath = Path.Combine(_folder, $"{identity.Id}.{identity.Version}.nupkg");
            using (var fileStream = File.Create(packagePath))
            {
                stream.Position = 0;
                await stream.CopyToAsync(fileStream);
            }

            return packageReader.NuspecReader.ToPackageInfos();
        }

        public Task RemoveAsync(string id, Version version)
        {
            var nugetVersion = new NuGetVersion(version);
            string packagePath = Path.Combine(_folder, $"{id}.{nugetVersion}.nupkg");

            if (File.Exists(packagePath))
            {
                File.Delete(packagePath);
            }

            return Task.CompletedTask;
        }

        // XXX : should be cached since we have to load the package and dll and check types dynamically
        public async Task<IEnumerable<ClassIdentifier>> GetTaskClassesAsync(string id, Version version)
        {
            var findPackageByIdResource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            var nugetVersion = new NuGetVersion(version);

            using var packageStream = new MemoryStream();
            await findPackageByIdResource.CopyNupkgToStreamAsync(
                id,
                nugetVersion,
                packageStream,
                _cacheContext,
                _logger,
                CancellationToken.None);

            packageStream.Position = 0;
            using var packageReader = new PackageArchiveReader(packageStream);

            // Get the nearest framework
            var nearestFramework = GetNearestFramework(packageReader);

            // Get only the dll files from the nearest framework folder
            var dllFiles = packageReader.GetFiles()
                .Where(f => f.StartsWith($"lib/{nearestFramework}/") && f.EndsWith(".dll"));

            // Load all the assemblies and get all the types that implement the ITask interface
            var taskClasses = new List<ClassIdentifier>();
            foreach (var dllFile in dllFiles)
            {
                using var dllStream = packageReader.GetStream(dllFile);
                using var memoryStream = new MemoryStream();
                dllStream.CopyTo(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());

                var taskTypes = assembly.GetTypes()
                    .Where(t => typeof(ITask).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                taskClasses.AddRange(taskTypes.Select(t => new ClassIdentifier(dllFile, t.FullName ?? "")));
            }

            return taskClasses;
        }

        public async Task<ITask> CreateTaskInstanceAsync(PackageClassTarget package)
        {
            // Get package by id resource
            var findPackageByIdResource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            var nugetVersion = new NuGetVersion(package.Package.Version);

            using var packageStream = new MemoryStream();
            await findPackageByIdResource.CopyNupkgToStreamAsync(
                package.Package.Identifier,
                nugetVersion,
                packageStream,
                _cacheContext,
                _logger,
                CancellationToken.None);

            packageStream.Position = 0;
            using var packageReader = new PackageArchiveReader(packageStream);

            var dllPath = packageReader.GetFiles()
                .FirstOrDefault(f => f.EndsWith(package.TargetClass.Dll))
                ?? throw new Exception($"Could not find main DLL for package '{package.Package.Identifier}' and dll '{package.TargetClass.Dll}'.");

            // Load the assembly
            using var dllStream = packageReader.GetStream(dllPath);
            using var memoryStream = new MemoryStream();
            await dllStream.CopyToAsync(memoryStream);

            var assembly = Assembly.Load(memoryStream.ToArray());
            var taskType = assembly.GetType(package.TargetClass.Name)
                ?? throw new Exception($"Could not find type '{package.TargetClass.Name}'.");

            if (!typeof(ITask).IsAssignableFrom(taskType))
            {
                throw new Exception($"Type '{package.TargetClass.Name}' does not implement ITask interface.");
            }

            return (Activator.CreateInstance(taskType) as ITask)
                ?? throw new Exception($"Failed to create instance of type '{package.TargetClass.Name}'.");
        }

        /// <summary>
        /// Get the nearest framework to the current assembly
        /// </summary>
        /// <param name="packageReader"></param>
        /// <returns></returns>
        private string GetNearestFramework(PackageArchiveReader packageReader)
        {
            var currentFramework = NuGetFramework.Parse(
                Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName ??
                    throw new Exception("Cannot determine the current version of .net"));
            var frameworks = packageReader.GetLibItems()
                .Select(group => group.TargetFramework)
                .Where(f => f != null)
                .ToList();

            var nearestFramework = NuGetFrameworkUtility.GetNearest(frameworks, currentFramework, f => f) ?? throw new Exception("Can't find the nearest framework.");

            return nearestFramework.GetShortFolderName();
        }
    }

    /// <summary>
    /// Extension method to convert to PackageInfos (you'll need to implement this based on your PackageInfos class)
    /// </summary>
    public static class PackageExtensions
    {
        public static PackageInfos ToPackageInfos(this IPackageSearchMetadata package)
        {
            return new PackageInfos()
            {
                Identifier = new PackageIdentifier()
                {
                    Identifier = package.Identity.Id,
                    Version = package.Identity.Version.Version
                },
                Description = package.Description
            };
        }
        public static PackageInfos ToPackageInfos(this NuspecReader reader)
        {
            return new PackageInfos()
            {
                Identifier = new PackageIdentifier()
                {
                    Identifier = reader.GetId(),
                    Version = reader.GetVersion().Version
                },
                Description = reader.GetDescription()
            };
        }
    }
}
