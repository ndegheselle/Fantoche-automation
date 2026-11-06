using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Packages;

/// <summary>
/// The tasks of the packages, each assembly loaded once and shared by every execution of a worker,
/// the branches of a workflow included.
/// </summary>
public sealed class TaskLoaders : IDisposable
{
    private readonly IPackageManagement _packages;

    /// <summary>Keyed by the path of their dll.</summary>
    private readonly Dictionary<string, TaskLoader> _loaders = [];

    public TaskLoaders(IPackageManagement packages)
    {
        _packages = packages;
    }

    public void Dispose()
    {
        lock (_loaders)
        {
            foreach (TaskLoader loader in _loaders.Values)
                loader.Dispose();
            _loaders.Clear();
        }
    }

    /// <summary>A new instance of the class [target] points at, its package downloaded if needed.</summary>
    /// <exception cref="PackageDownloadException">The package or its assembly can't be resolved.</exception>
    public async Task<ITask> CreateAsync(PackageClassTarget target)
    {
        string dllPath = await _packages.DownloadPackageAsync(target.Package.Id, target.Package.Version, target.Dll);

        TaskLoader loader;
        lock (_loaders)
        {
            if (!_loaders.TryGetValue(dllPath, out loader!))
            {
                loader = new TaskLoader(dllPath);
                _loaders.Add(dllPath, loader);
            }
        }

        return loader.CreateInstance(target.ClassFullName);
    }
}
