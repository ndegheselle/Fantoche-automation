using Fantoche.Plugins.Shared;
using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using Fantoche.Worker.Packages;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace Fantoche.Services.Local;

/// <summary>
/// The packages of the supervisor, kept in a folder, and the assemblies extracted from them to
/// check what they hold. The workers extract their own from its files (see <see cref="IPackageSource"/>).
/// </summary>
public class LocalPackagesService : IPackagesService, IPackageSource
{
    private readonly PackageFolder _folder;
    private readonly PackageCache _cache;

    /// <param name="loadSymbols">
    /// Whether the symbol packages are extracted along with the assemblies, so the tasks can be
    /// stepped into. Defaults to whether the application itself is a debug build.
    /// </param>
    public LocalPackagesService(string folderPath, string cacheFolderPath, bool? loadSymbols = null)
    {
        _folder = new PackageFolder(folderPath);
        _cache = new PackageCache(_folder, cacheFolderPath, loadSymbols);
    }

    /// <summary>The ".nupkg" of [id] [version], null when there is none.</summary>
    public Task<Stream?> OpenPackageAsync(string id, Version version) => _folder.OpenPackageAsync(id, version);

    /// <summary>The ".snupkg" of [id] [version], null when there is none.</summary>
    public Task<Stream?> OpenSymbolsAsync(string id, Version version) => _folder.OpenSymbolsAsync(id, version);

    public Task<Paginated<PackageInfos>> SearchAsync(string search = "", PaginationOptions options = default)
    {
        return _folder.SearchAsync(search, options);
    }

    public async Task<PackageAdded> AddAsync(string filePath)
    {
        var infos = await _folder.AddAsync(filePath);
        // Only a task target goes without a version, a package always has one.
        Version version = infos.Identifier.Version!;

        // A symbol package only makes the tasks of the package it belongs to debuggable, there is
        // nothing to look for in it.
        if (infos.IsSymbols)
        {
            await _cache.ExtractSymbolsAsync(infos.Identifier.Id, version);
            return new PackageAdded() { Infos = infos };
        }

        // The interfaces of the tasks are for Windows : only the application loads them, and lists
        // them itself (see TaskInterfaces in Fantoche.App).
        if (await IsPlatformOnlyAsync(infos.Identifier.Id, version))
            return new PackageAdded() { Infos = infos };

        // Check if the package contain tasks
        var dllsPaths = await _cache.DownloadPackageAsync(infos.Identifier.Id, version);
        List<string> classes = [];
        foreach (var path in dllsPaths)
        {
            using TaskLoader loader = new TaskLoader(path);
            classes.AddRange(loader.GetClasses());
        }

        List<Warning> warnings = [];
        if (classes.Count == 0)
            warnings = [new Warning("packages.add.warnings.noTasks", "This package doesn't contain any compatible task.")];

        return new PackageAdded()
        {
            Infos = infos,
            Warnings = warnings,
        };
    }

    public Task RemoveAsync(string id, Version? version)
    {
        return _folder.RemoveAsync(id, version);
    }

    public Task<IEnumerable<Version>> GetVersionsAsync(string id)
    {
        return _folder.GetVersionsAsync(id);
    }

    public async Task<List<ClassTarget>> GetClassesAsync(string id, Version version)
    {
        var identifier = new PackageIdentifier { Id = id, Version = version };
        if (await IsPlatformOnlyAsync(id, version))
            return [];

        var dllsPaths = await _cache.DownloadPackageAsync(id, version);

        List<ClassTarget> targets = [];
        foreach (var path in dllsPaths)
        {
            // Without its extension : the worker looks the assembly up as "{Dll}.dll".
            string dll = Path.GetFileNameWithoutExtension(path);
            using TaskLoader loader = new TaskLoader(path);
            foreach (Type type in loader.GetTypes())
            {
                targets.Add(new PackageClassTarget(identifier, type.FullName ?? "") { Dll = dll });
            }
        }

        return targets;
    }

    /// <summary>Whether [id] [version] only holds assemblies for a platform, see <see cref="PackageCache.IsPlatformOnly"/>.</summary>
    private async Task<bool> IsPlatformOnlyAsync(string id, Version version)
    {
        await using Stream? package = await _folder.OpenPackageAsync(id, version);
        if (package == null)
            return false;

        using PackageArchiveReader reader = new(package);
        List<NuGetFramework> frameworks = [.. reader.GetLibItems().Select(x => x.TargetFramework)];
        return PackageCache.IsPlatformOnly(frameworks);
    }
}