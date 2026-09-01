using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Automation.Worker.Packages;

namespace Automation.Services.Local;

public class LocalPackagesService : IPackagesService
{
    private readonly LocalPackageManagement _packages;

    /// <summary>
    /// The packages the tasks are loaded from, shared with whoever executes them.
    /// </summary>
    public LocalPackageManagement PackageManagement => _packages;

    /// <param name="loadSymbols">
    /// Whether the symbol packages are extracted along with the assemblies, so the tasks can be
    /// stepped into. Defaults to whether the application itself is a debug build.
    /// </param>
    public LocalPackagesService(string folderPath, string cacheFolderPath, bool? loadSymbols = null)
    {
        _packages = new LocalPackageManagement(folderPath, cacheFolderPath, loadSymbols);
    }

    public Task<Paginated<PackageInfos>> SearchAsync(string search = "", PaginationOptions options = default)
    {
        return _packages.SearchAsync(search, options);
    }

    public async Task<PackageAdded> AddAsync(string filePath)
    {
        var infos = await _packages.AddAsync(filePath);

        // A symbol package only makes the tasks of the package it belongs to debuggable, there is
        // nothing to look for in it.
        if (infos.IsSymbols)
            return new PackageAdded() { Infos = infos };

        // Check if the package contain tasks
        var dllsPaths = await _packages.DownloadPackageAsync(infos.Identifier.Id, infos.Identifier.Version);
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
        return _packages.RemoveAsync(id, version);
    }

    public Task<IEnumerable<Version>> GetVersionsAsync(string id)
    {
        return _packages.GetVersionsAsync(id);
    }

    public async Task<List<ClassTarget>> GetClassesAsync(string id, Version version)
    {
        var identifier = new PackageIdentifier { Id = id, Version = version };
        var dllsPaths = await _packages.DownloadPackageAsync(id, version);

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