using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Automation.Worker.Packages;

namespace Automation.Services.Local;

public class LocalPackagesService : IPackagesService
{
    private readonly LocalPackageManagement _packages;

    public LocalPackagesService(string folder)
    {
        _packages = new LocalPackageManagement(folder);
    }

    public Task<Paginated<PackageInfos>> SearchAsync(string search = "", PaginationOptions options = default)
    {
        return _packages.SearchAsync(search, options);
    }

    public async Task<PackageAdded> AddAsync(string filePath)
    {
        var infos = await _packages.AddAsync(filePath);

        // The package assemblies are loaded (and validated for tasks) once by the tasks
        // service when it builds the catalog tasks, so we don't enumerate them here.
        return new PackageAdded()
        {
            Infos = infos,
            Warnings = [],
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
        var dllsPaths = await _packages.DownloadAllDllsIfMissing(id, version);

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