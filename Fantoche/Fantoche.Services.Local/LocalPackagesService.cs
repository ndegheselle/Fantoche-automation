using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using Fantoche.Worker.Packages;

namespace Fantoche.Services.Local;

/// <summary>
/// The packages of a folder, and the assemblies extracted from them to check what they hold : the
/// ones of the supervisor, or of the application in standalone mode.
/// </summary>
public class LocalPackagesService : IPackagesService
{
    private readonly PackageFolder _folder;
    private readonly PackageCache _cache;

    /// <summary>
    /// The assemblies the tasks are loaded from, for whoever executes them on this machine.
    /// </summary>
    public PackageCache Cache => _cache;

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

        // A symbol package only makes the tasks of the package it belongs to debuggable, there is
        // nothing to look for in it.
        if (infos.IsSymbols)
        {
            await _cache.ExtractSymbolsAsync(infos.Identifier.Id, infos.Identifier.Version);
            return new PackageAdded() { Infos = infos };
        }

        // Check if the package contain tasks
        var dllsPaths = await _cache.DownloadPackageAsync(infos.Identifier.Id, infos.Identifier.Version);
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
        var dllsPaths = await _cache.DownloadPackageAsync(id, version);

        List<ClassTarget> targets = [];
        foreach (var path in dllsPaths)
        {
            string dll = Path.GetFileName(path);
            using TaskLoader loader = new TaskLoader(path);
            foreach (var className in loader.GetClasses())
                targets.Add(new PackageClassTarget(identifier, className) { Dll = dll });
        }

        return targets;
    }
}