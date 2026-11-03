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
        (ITask task, _) = await LoadAsync(target);
        return task;
    }

    /// <summary>
    /// The folder holding the page of the user interface task [target] points at, its package
    /// downloaded if needed.
    /// </summary>
    /// <exception cref="PackageDownloadException">The package or its assembly can't be resolved.</exception>
    /// <exception cref="InvalidOperationException">[target] isn't a user interface task.</exception>
    public async Task<string> GetPageFolderAsync(PackageClassTarget target)
    {
        (ITask task, string dllPath) = await LoadAsync(target);
        if (task is not IUserInterfaceTask userInterface)
            throw new InvalidOperationException($"[{target.ClassFullName}] has no page : it isn't a user interface task.");

        string assemblyFolder = Path.GetDirectoryName(dllPath) ?? throw new InvalidOperationException($"[{dllPath}] has no folder.");
        return Path.GetFullPath(Path.Combine(assemblyFolder, userInterface.PageFolder));
    }

    /// <summary>
    /// The task with the dll it was loaded from : a target following the newest version could
    /// resolve another one if its package was downloaded a second time.
    /// </summary>
    private async Task<(ITask Task, string DllPath)> LoadAsync(PackageClassTarget target)
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

        return (loader.CreateInstance(target.ClassFullName), dllPath);
    }
}
