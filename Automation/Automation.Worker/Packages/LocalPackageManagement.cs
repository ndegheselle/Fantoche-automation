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

namespace Automation.Worker.Packages;

public class LocalPackageManagement : IPackageManagement
{
    private readonly string _folder;
    private readonly string _localFolder;
    private readonly SourceRepository _repository;
    private readonly SourceCacheContext _cacheContext;
    private readonly ILogger _logger;
    private readonly NuGetFramework _frameworkVersion;

    public LocalPackageManagement(string folder)
    {
        _frameworkVersion = GetCurrentFramework();
        _localFolder = Path.Combine(Directory.GetCurrentDirectory(), "packages");
        _folder = folder;
        var packageSource = new PackageSource(folder);
        _repository = Repository.Factory.GetCoreV3(packageSource);
        _cacheContext = new SourceCacheContext();
        _logger = NullLogger.Instance;
    }

    public async Task<ListPageWrapper<PackageInfos>> SearchAsync(string name = "", int page = 1, int pageSize = 50)
    {
        // XXX : difference with LocalPackageSearchResource ?
        var resource = _repository.GetResource<PackageSearchResource>();
        var packages = await resource.SearchAsync(
            name,
            new SearchFilter(false),
            page - 1,
            pageSize,
            NullLogger.Instance,
            CancellationToken.None);
        return new ListPageWrapper<PackageInfos>
        {
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
            metadata = await resource.GetMetadataAsync(new PackageIdentity(packageId, new NuGetVersion(version)),
                _cacheContext, _logger, CancellationToken.None);
        }
        else
        {
            IEnumerable<IPackageSearchMetadata> versions = await resource.GetMetadataAsync(packageId, true, false,
                _cacheContext, _logger, CancellationToken.None);

            var latestVersion = versions.MaxBy(x => x.Identity.Version) ??
                                throw new Exception($"No package found for the id {packageId}");
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
        var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
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

        if (File.Exists(packagePath)) File.Delete(packagePath);

        return Task.CompletedTask;
    }

    public async Task<string> DownloadPackageAsync(string id, Version version)
    {
        // Get package by id resource
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
        var nearestFramework = GetNearestFramework(packageReader);
        var lib = packageReader.GetLibItems().FirstOrDefault(x => x.TargetFramework == nearestFramework);

        var extractedFiles = await packageReader.CopyFilesAsync(_localFolder, lib.Items,
            (source, target, stream) => ExtractFile(id, version, target, stream), _logger, CancellationToken.None);

        return GetLocalDllPath(id, version) ?? throw new Exception($"Could not find dll for [id:{id}][version:{version}] in downloaded package.");
    }

    private string ExtractFile(string id, Version version, string targetPath, Stream fileStream)
    {
        string fileName = Path.GetFileName(targetPath);
        string folderPath = GetLocalFolderPath(id, version);
        string path = Path.Combine(folderPath, fileName);
        Directory.CreateDirectory(folderPath);
        using var targetStream = File.Create(path);
        fileStream.CopyTo(targetStream);
        return path;
    }

    public async Task<string> DownloadToLocalIfMissing(string id, Version version)
    {
        string? path = GetLocalDllPath(id, version);
        if (string.IsNullOrEmpty(path) == false)
            return path;
        return await DownloadPackageAsync(id, version);
    }

    public string? GetLocalDllPath(string id, Version version)
    {
        string path = Path.Combine(GetLocalFolderPath(id, version), $"{id}.dll");
        return File.Exists(path) == false ? null : path;
    }

    private string GetLocalFolderPath(string id, Version version)
    {
        return Path.Combine(_localFolder, id, version.ToString());
    }


    /// <summary>
    /// Get the nearest framework to the current assembly
    /// </summary>
    /// <param name="packageReader"></param>
    /// <returns></returns>
    private NuGetFramework GetNearestFramework(PackageArchiveReader packageReader)
    {
        var frameworks = packageReader.GetLibItems()
            .Select(group => group.TargetFramework)
            .Where(f => f != null)
            .ToList();

        return NuGetFrameworkUtility.GetNearest(frameworks, _frameworkVersion, f => f)
               ?? throw new Exception("Can't find the nearest framework.");
    }

    /// <summary>
    /// Get the current assembly framework version.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private NuGetFramework GetCurrentFramework()
    {
        return NuGetFramework.Parse(
            Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName ??
            throw new Exception("Cannot determine the current version of .net"));
    }
}

/// <summary>
/// Extension method to convert to PackageInfos (you'll need to implement this based on your PackageInfos class)
/// </summary>
public static class PackageExtensions
{
    public static PackageInfos ToPackageInfos(this IPackageSearchMetadata package)
    {
        return new PackageInfos
        {
            Identifier = new PackageIdentifier
            {
                Identifier = package.Identity.Id,
                Version = package.Identity.Version.Version
            },
            Description = package.Description
        };
    }

    public static PackageInfos ToPackageInfos(this NuspecReader reader)
    {
        return new PackageInfos
        {
            Identifier = new PackageIdentifier
            {
                Identifier = reader.GetId(),
                Version = reader.GetVersion().Version
            },
            Description = reader.GetDescription()
        };
    }
}