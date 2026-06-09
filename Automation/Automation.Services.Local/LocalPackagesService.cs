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
        
        // Check if the package contain tasks
        var dllsPaths = await _packages.DownloadAllDllsIfMissing(infos.Identifier.Id, infos.Identifier.Version);
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

    public Task RemoveAsync(string id, Version version)
    {
        return _packages.RemoveAsync(id, version);
    }
}