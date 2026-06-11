using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Versioning;
using Automation.Shared.Services;

namespace Automation.Worker.Packages;

#region Exceptions

public class PackageDownloadException : Exception
{
    public PackageDownloadException(string message) : base(message) { }
    public PackageDownloadException(string message, Exception ex) : base(message, ex) { }
}

#endregion

public class LocalPackageManagement
{
    #region Fields

    private readonly string _folder;
    private readonly string _localFolder;
    private readonly Task<SourceRepository> _repositoryTask;
    private readonly SourceCacheContext _cacheContext;
    private readonly ILogger _logger;
    private readonly NuGetFramework _frameworkVersion;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _downloadLocks = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="LocalPackageManagement"/>.
    /// </summary>
    /// <param name="folder">Path to the local NuGet package source folder.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="folder"/> does not exist.</exception>
    public LocalPackageManagement(string folder)
    {
        _frameworkVersion = GetCurrentFramework();
        _localFolder = Path.Combine(Directory.GetCurrentDirectory(), "packages");
        _folder = folder;

        if (Directory.Exists(_folder) == false)
            Directory.CreateDirectory(_folder);

        var packageSource = new PackageSource(folder);
        _repositoryTask = Task.Run(() => Repository.Factory.GetCoreV3(packageSource));
        _cacheContext = new SourceCacheContext();
        _logger = NullLogger.Instance;
    }

    #endregion

    #region Search

    /// <summary>
    /// Searches for packages in the repository by name with pagination support.
    /// </summary>
    /// <param name="name">The search term to filter packages by name. Defaults to empty (returns all).</param>
    /// <param name="options">Pagination options (page number and page size).</param>
    /// <returns>A paginated list of matching <see cref="PackageInfos"/>.</returns>
    public async Task<Paginated<PackageInfos>> SearchAsync(string name = "", PaginationOptions options = default)
    {
        // XXX : difference with LocalPackageSearchResource ?
        var resource = (await _repositoryTask).GetResource<PackageSearchResource>();
        var packages = await resource.SearchAsync(
            name,
            new SearchFilter(false),
            options.Page - 1,
            options.PageSize,
            NullLogger.Instance,
            CancellationToken.None);

        return new Paginated<PackageInfos>
        {
            Options = options,
            Total = -1,
            Items = packages.Select(x => x.ToPackageInfos()).ToList()
        };
    }

    /// <summary>
    /// Retrieves metadata for a specific package.
    /// If <paramref name="version"/> is <c>null</c>, returns the latest available version.
    /// </summary>
    /// <param name="packageId">The NuGet package identifier.</param>
    /// <param name="version">The requested version, or <c>null</c> to get the latest.</param>
    /// <returns>The <see cref="PackageInfos"/> for the resolved package.</returns>
    /// <exception cref="Exception">Thrown if no package is found for the given identifier.</exception>
    public async Task<PackageInfos> GetInfosAsync(string packageId, Version? version)
    {
        var resource = await (await _repositoryTask).GetResourceAsync<PackageMetadataResource>();
        IPackageSearchMetadata metadata;

        if (version != null)
        {
            metadata = await resource.GetMetadataAsync(
                new PackageIdentity(packageId, new NuGetVersion(version)),
                _cacheContext, _logger, CancellationToken.None);
        }
        else
        {
            IEnumerable<IPackageSearchMetadata> versions = await resource.GetMetadataAsync(
                packageId, true, false, _cacheContext, _logger, CancellationToken.None);

            metadata = versions.MaxBy(x => x.Identity.Version)
                ?? throw new Exception($"No package found for the id {packageId}");
        }

        return metadata.ToPackageInfos();
    }

    /// <summary>
    /// Returns all available versions for a given package, sorted from newest to oldest.
    /// </summary>
    /// <param name="packageId">The NuGet package identifier.</param>
    /// <returns>An enumerable of <see cref="Version"/> sorted in descending order.</returns>
    public async Task<IEnumerable<Version>> GetVersionsAsync(string packageId)
    {
        var resource = await (await _repositoryTask).GetResourceAsync<FindPackageByIdResource>();

        IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
            packageId,
            _cacheContext,
            _logger,
            CancellationToken.None);

        return versions.Select(x => x.Version).Reverse();
    }

    #endregion

    #region Add / Create

    /// <summary>
    /// Adds a package to the repository from a file on disk.
    /// </summary>
    /// <param name="filePath">Absolute path to the <c>.nupkg</c> file to add.</param>
    /// <returns>The <see cref="PackageInfos"/> of the added package.</returns>
    /// <exception cref="PackageValidationException">Thrown if the file is not a valid NuGet package, or if it already exists in the repository.</exception>
    public async Task<PackageInfos> AddAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        return await CreateFromStreamAsync(stream);
    }

    /// <summary>
    /// Adds a package to the repository from a stream.
    /// The stream must represent a valid <c>.nupkg</c> archive.
    /// </summary>
    /// <param name="stream">The stream containing the NuGet package data.</param>
    /// <returns>The <see cref="PackageInfos"/> of the newly added package.</returns>
    /// <exception cref="PackageValidationException">
    /// Thrown if the stream is not a valid NuGet package, or if the package version already exists in the repository.
    /// </exception>
    public async Task<PackageInfos> CreateFromStreamAsync(Stream stream)
    {
        if (!IsValidNugetPackage(stream))
            throw new PackageValidationException("Invalid nuget package.");

        stream.Position = 0;

        using var packageReader = new PackageArchiveReader(stream);
        var identity = await packageReader.GetIdentityAsync(CancellationToken.None);

        string packagePath = Path.Combine(_folder, $"{identity.Id}.{identity.Version}.nupkg");

        if (File.Exists(packagePath))
            throw new PackageValidationException($"The package '{identity.Id}' (version {identity.Version}) already exists.");

        await using (var fileStream = File.Create(packagePath))
        {
            stream.Position = 0;
            await stream.CopyToAsync(fileStream);
        }

        return packageReader.NuspecReader.ToPackageInfos();
    }

    #endregion

    #region Remove

    /// <summary>
    /// Removes a package from the repository.
    /// If <paramref name="version"/> is <c>null</c>, all versions of the package are removed.
    /// If the corresponding <c>.nupkg</c> file does not exist, the operation is silently skipped.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The version of the package to remove, or <c>null</c> to remove all versions.</param>
    public Task RemoveAsync(string id, Version? version)
    {
        if (version is null)
        {
            foreach (string packagePath in Directory.EnumerateFiles(_folder, $"{id}.*.nupkg"))
                File.Delete(packagePath);
        }
        else
        {
            var nugetVersion = new NuGetVersion(version);
            string packagePath = Path.Combine(_folder, $"{id}.{nugetVersion.ToNormalizedString()}.nupkg");

            if (File.Exists(packagePath))
                File.Delete(packagePath);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Download

    /// <summary>
    /// Downloads a package from the repository and extracts all compatible DLL files
    /// to the local packages folder, then returns the paths of every extracted DLL.
    /// This is the core download implementation shared by all public download methods.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The version of the package to download.</param>
    /// <returns>An enumerable of absolute paths to every extracted <c>.dll</c> file.</returns>
    /// <exception cref="PackageDownloadException">
    /// Thrown if the package is not found, no compatible framework is available,
    /// or any other download error occurs.
    /// </exception>
    private async Task<IEnumerable<string>> DownloadPackageDllsAsync(string id, Version version)
    {
        try
        {
            var findPackageByIdResource = await (await _repositoryTask).GetResourceAsync<FindPackageByIdResource>();
            var nugetVersion = new NuGetVersion(version);

            using var packageStream = new MemoryStream();
            bool found = await findPackageByIdResource.CopyNupkgToStreamAsync(
                id,
                nugetVersion,
                packageStream,
                _cacheContext,
                _logger,
                CancellationToken.None);

            if (!found || packageStream.Length == 0)
                throw new PackageDownloadException($"Package not found in repository [id:{id}][version:{nugetVersion}]");

            packageStream.Position = 0;
            using var packageReader = new PackageArchiveReader(packageStream);
            var nearestFramework = GetNearestFramework(packageReader);
            var lib = packageReader.GetLibItems().FirstOrDefault(x => x.TargetFramework == nearestFramework);

            if (lib == null)
                throw new PackageDownloadException($"Could not find compatible nearest framework [nearest:{nearestFramework}]");

            await packageReader.CopyFilesAsync(
                _localFolder,
                lib.Items,
                (source, target, stream) => ExtractFile(id, version, target, stream),
                _logger,
                CancellationToken.None);

            return GetLocalDllPaths(id, version);
        }
        catch (Exception ex)
        {
            throw new PackageDownloadException($"Failed to download package [id:{id}][version:{version}]: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Downloads a package from the repository and returns the path of a specific DLL.
    /// Delegates extraction to <see cref="DownloadPackageDllsAsync"/> then resolves the requested entry.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The version of the package to download.</param>
    /// <param name="dll">The name of the DLL (without extension) expected inside the package.</param>
    /// <returns>The full local path to the extracted DLL.</returns>
    /// <exception cref="PackageDownloadException">
    /// Thrown if the package is not found, no compatible framework is available,
    /// the expected DLL is missing, or any other download error occurs.
    /// </exception>
    public async Task<string> DownloadPackageAsync(string id, Version version, string dll)
    {
        await DownloadPackageDllsAsync(id, version);
        return GetLocalDllPath(id, version, dll)
            ?? throw new PackageDownloadException($"Could not find dll for [id:{id}][version:{version}] in downloaded package.");
    }

    /// <summary>
    /// Returns the path of a specific DLL if it is already cached locally,
    /// otherwise downloads the package first.
    /// Uses a per-package semaphore to prevent concurrent duplicate downloads.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The version of the package.</param>
    /// <param name="dll">The name of the DLL (without extension) to locate.</param>
    /// <returns>The full local path to the DLL.</returns>
    public async Task<string> DownloadToLocalIfMissing(string id, Version version, string dll)
    {
        string? path = GetLocalDllPath(id, version, dll);
        if (!string.IsNullOrEmpty(path))
            return path;

        string key = $"{id}.{version}";
        var semaphore = _downloadLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            // Re-check after acquiring the lock — another task may have already downloaded it.
            path = GetLocalDllPath(id, version, dll);
            if (!string.IsNullOrEmpty(path))
                return path;

            return await DownloadPackageAsync(id, version, dll);
        }
        finally
        {
            semaphore.Release();
            _downloadLocks.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Returns all DLL paths for a package if already cached locally,
    /// otherwise downloads the package first.
    /// Uses a per-package semaphore to prevent concurrent duplicate downloads.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The version of the package.</param>
    /// <returns>An enumerable of absolute paths to every <c>.dll</c> file in the package.</returns>
    public async Task<IEnumerable<string>> DownloadAllDllsIfMissing(string id, Version version)
    {
        var paths = GetLocalDllPaths(id, version);
        if (paths.Any())
            return paths;

        string key = $"{id}.{version}";
        var semaphore = _downloadLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            // Re-check after acquiring the lock — another task may have already downloaded it.
            paths = GetLocalDllPaths(id, version);
            if (paths.Any())
                return paths;

            return await DownloadPackageDllsAsync(id, version);
        }
        finally
        {
            semaphore.Release();
            _downloadLocks.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Extracts a single file from a package archive to the local versioned folder.
    /// </summary>
    /// <param name="id">The NuGet package identifier (used to build the destination path).</param>
    /// <param name="version">The package version (used to build the destination path).</param>
    /// <param name="targetPath">The relative target path as declared inside the archive.</param>
    /// <param name="fileStream">The stream for the file to extract.</param>
    /// <returns>The full path where the file was written.</returns>
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

    #endregion

    #region Local File Resolution

    /// <summary>
    /// Returns the full path to a DLL in the local package cache, or <c>null</c> if it has not been extracted yet.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <param name="dll">The name of the DLL (without extension).</param>
    /// <returns>The full path if the file exists; otherwise <c>null</c>.</returns>
    public string? GetLocalDllPath(string id, Version version, string dll)
    {
        string path = Path.Combine(GetLocalFolderPath(id, version), $"{dll}.dll");
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Returns the full paths of all DLLs extracted for a given package version in the local cache.
    /// Returns an empty enumerable if the package has not been downloaded yet.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <returns>An enumerable of absolute paths to every <c>.dll</c> file found in the local package folder.</returns>
    public IEnumerable<string> GetLocalDllPaths(string id, Version version)
    {
        string folder = GetLocalFolderPath(id, version);
        if (!Directory.Exists(folder))
            return Enumerable.Empty<string>();

        return Directory.EnumerateFiles(folder, "*.dll");
    }

    /// <summary>
    /// Builds the local folder path for a specific package version in the cache.
    /// </summary>
    /// <param name="id">The NuGet package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <returns>The absolute folder path for the package version.</returns>
    private string GetLocalFolderPath(string id, Version version)
    {
        var nugetVersion = new NuGetVersion(version);
        return Path.Combine(_localFolder, id, nugetVersion.ToNormalizedString());
    }

    #endregion

    #region Validation

    /// <summary>
    /// Reads package metadata from a stream and returns it.
    /// Used internally to validate and inspect a <c>.nupkg</c> archive.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The <see cref="PackageInfos"/> extracted from the archive.</returns>
    private PackageInfos GetInfosFromStream(Stream stream)
    {
        using var packageReader = new PackageArchiveReader(stream, leaveStreamOpen: true);
        return packageReader.NuspecReader.ToPackageInfos();
    }

    /// <summary>
    /// Determines whether the given stream represents a valid NuGet package archive.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <returns><c>true</c> if the stream is a readable NuGet package; otherwise <c>false</c>.</returns>
    private bool IsValidNugetPackage(Stream stream)
    {
        try
        {
            _ = GetInfosFromStream(stream);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Framework Resolution

    /// <summary>
    /// Selects the target framework from the package that is nearest to the current runtime framework.
    /// </summary>
    /// <param name="packageReader">The archive reader for the package being inspected.</param>
    /// <returns>The best matching <see cref="NuGetFramework"/>.</returns>
    /// <exception cref="Exception">Thrown if no compatible framework can be found in the package.</exception>
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
    /// Resolves the <see cref="NuGetFramework"/> of the currently running application
    /// by reading the <see cref="TargetFrameworkAttribute"/> on the entry assembly.
    /// </summary>
    /// <returns>The <see cref="NuGetFramework"/> matching the current runtime.</returns>
    /// <exception cref="Exception">Thrown if the target framework attribute is not found on the entry assembly.</exception>
    private NuGetFramework GetCurrentFramework()
    {
        return NuGetFramework.Parse(
            Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName
            ?? throw new Exception("Cannot determine the current version of .net"));
    }

    #endregion
}

/// <summary>
/// Extension methods to map NuGet metadata types to <see cref="PackageInfos"/>.
/// </summary>
public static class PackageExtensions
{
    /// <summary>
    /// Converts a <see cref="IPackageSearchMetadata"/> result to a <see cref="PackageInfos"/> instance.
    /// </summary>
    /// <param name="package">The search metadata to convert.</param>
    /// <returns>A populated <see cref="PackageInfos"/>.</returns>
    public static PackageInfos ToPackageInfos(this IPackageSearchMetadata package)
    {
        return new PackageInfos
        {
            Identifier = new PackageIdentifier
            {
                Id = package.Identity.Id,
                Version = package.Identity.Version.Version
            },
            Description = package.Description
        };
    }

    /// <summary>
    /// Converts a <see cref="NuspecReader"/> to a <see cref="PackageInfos"/> instance.
    /// </summary>
    /// <param name="reader">The nuspec reader to convert.</param>
    /// <returns>A populated <see cref="PackageInfos"/>.</returns>
    public static PackageInfos ToPackageInfos(this NuspecReader reader)
    {
        return new PackageInfos
        {
            Identifier = new PackageIdentifier
            {
                Id = reader.GetId(),
                Version = reader.GetVersion().Version
            },
            Description = reader.GetDescription(),
        };
    }
}