using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Fantoche.Worker.Packages;

/// <summary>
/// The folder the packages are added to, read as a nuget source : kept by the supervisor, or by the
/// application in standalone mode. The ones running the tasks only read the files, through a
/// <see cref="PackageCache"/>.
/// </summary>
public class PackageFolder : IPackageSource
{
    /// <summary>
    /// Extension of a symbol package, holding the pdb files of the package of the same identifier.
    /// </summary>
    public const string SymbolsExtension = ".snupkg";

    private const string PackageExtension = ".nupkg";

    private readonly string _folder;
    private readonly Task<SourceRepository> _repositoryTask;
    private readonly SourceCacheContext _cacheContext = new();

    public PackageFolder(string folderPath)
    {
        _folder = folderPath;
        Directory.CreateDirectory(_folder);

        var packageSource = new PackageSource(folderPath);
        _repositoryTask = Task.Run(() => Repository.Factory.GetCoreV3(packageSource));
    }

    #region Search

    /// <summary>
    /// The packages whose name matches [name], every one when it is empty.
    /// </summary>
    public async Task<Paginated<PackageInfos>> SearchAsync(string name = "", PaginationOptions options = default)
    {
        var resource = await (await _repositoryTask).GetResourceAsync<PackageSearchResource>();
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
    /// Every version of [packageId], from the newest to the oldest.
    /// </summary>
    public async Task<IEnumerable<Version>> GetVersionsAsync(string packageId)
    {
        var resource = await (await _repositoryTask).GetResourceAsync<FindPackageByIdResource>();

        IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
            packageId,
            _cacheContext,
            NullLogger.Instance,
            CancellationToken.None);

        return versions.Select(x => x.Version).Reverse();
    }

    #endregion

    #region Add / Remove

    /// <summary>
    /// Add the package (or the symbols of one) at [filePath].
    /// <para>
    /// A version is never replaced : whoever runs the tasks keeps the assemblies it extracted once
    /// (see <see cref="PackageCache"/>), so a changed package must come with a new version.
    /// </para>
    /// </summary>
    /// <exception cref="PackageValidationException">The file is not a nuget package.</exception>
    /// <exception cref="InvalidOperationException">That version is already added.</exception>
    public async Task<PackageInfos> AddAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        if (!IsValidNugetPackage(stream))
            throw new PackageValidationException("Invalid nuget package.");

        stream.Position = 0;
        using var packageReader = new PackageArchiveReader(stream, leaveStreamOpen: true);
        var identity = await packageReader.GetIdentityAsync(CancellationToken.None);

        // A symbol package carries the pdb of the package of the same identifier : it is stored
        // next to it rather than as a package of its own, the local source only listing ".nupkg".
        PackageInfos infos = packageReader.NuspecReader.ToPackageInfos();
        string extension = infos.IsSymbols ? SymbolsExtension : PackageExtension;
        string packagePath = GetFilePath(identity.Id, identity.Version.Version, extension);
        if (File.Exists(packagePath))
            throw new InvalidOperationException($"[{identity.Id}] version [{identity.Version}] is already added, give the package a new version.");

        await using var fileStream = File.Create(packagePath);
        stream.Position = 0;
        await stream.CopyToAsync(fileStream);

        return infos;
    }

    /// <summary>
    /// Remove [version] of [id], or every version of it when [version] is null. Nothing happens when
    /// there is no such package.
    /// </summary>
    public Task RemoveAsync(string id, Version? version)
    {
        // The symbols belong to the package they carry the pdb of, they leave with it.
        if (version is null)
        {
            foreach (string packagePath in Directory.EnumerateFiles(_folder, $"{id}.*{PackageExtension}")
                .Concat(Directory.EnumerateFiles(_folder, $"{id}.*{SymbolsExtension}")))
                File.Delete(packagePath);
        }
        else
        {
            foreach (string packagePath in new[] { GetFilePath(id, version, PackageExtension), GetFilePath(id, version, SymbolsExtension) })
                if (File.Exists(packagePath))
                    File.Delete(packagePath);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Files

    public Task<Stream?> OpenPackageAsync(string id, Version version)
        => Task.FromResult(OpenFile(GetFilePath(id, version, PackageExtension)));

    public Task<Stream?> OpenSymbolsAsync(string id, Version version)
        => Task.FromResult(OpenFile(GetFilePath(id, version, SymbolsExtension)));

    private static Stream? OpenFile(string path) => File.Exists(path) ? File.OpenRead(path) : null;

    /// <summary>
    /// Named the way <c>dotnet pack</c> names it, so a package copied by hand into the folder is
    /// found as well.
    /// </summary>
    private string GetFilePath(string id, Version version, string extension)
    {
        var nugetVersion = new NuGetVersion(version);
        return Path.Combine(_folder, $"{id}.{nugetVersion.ToNormalizedString()}{extension}");
    }

    private static bool IsValidNugetPackage(Stream stream)
    {
        try
        {
            using var packageReader = new PackageArchiveReader(stream, leaveStreamOpen: true);
            _ = packageReader.NuspecReader.ToPackageInfos();
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Extension methods to map NuGet metadata types to <see cref="PackageInfos"/>.
/// </summary>
public static class PackageExtensions
{
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
            IsSymbols = reader.GetPackageTypes().Any(x => x == PackageType.SymbolsPackage),
        };
    }
}
