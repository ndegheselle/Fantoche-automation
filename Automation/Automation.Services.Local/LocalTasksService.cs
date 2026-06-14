using System.Security.Cryptography;
using System.Text;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Automation.Worker.Packages;

namespace Automation.Services.Local;

/// <summary>
/// In-memory implementation of <see cref="ITasksService"/>. It keeps the read-only
/// catalog tasks derived from packages in a static store (shared across instances until
/// a real persistence layer exists) and rebuilds them from the package dlls on demand.
/// </summary>
public class LocalTasksService : ITasksService
{
    /// <summary>
    /// Well-known scope the generated catalog tasks are parented to, so they can be
    /// listed/picked separately from user-authored elements.
    /// </summary>
    public static readonly Guid CATALOG_SCOPE_ID = new("00000000-0000-0000-0000-000000000002");

    private const string PackageTag = "Package";

    // Keyed by task id; the deterministic id lets us upsert idempotently.
    private static readonly Dictionary<Guid, AutomationTask> _catalog = [];
    private static readonly object _lock = new();

    private readonly LocalPackageManagement _packages;

    public LocalTasksService(string folder)
    {
        _packages = new LocalPackageManagement(folder);
    }

    public Task<List<AutomationTask>> GetPackageTasksAsync(string packageId = "")
    {
        lock (_lock)
        {
            var tasks = _catalog.Values
                .Where(x => string.IsNullOrEmpty(packageId) || x.Target?.Package.Id == packageId)
                .ToList();
            return Task.FromResult(tasks);
        }
    }

    public async Task SyncPackageAsync(string packageId)
    {
        var latest = (await _packages.GetVersionsAsync(packageId)).FirstOrDefault();
        if (latest == null)
        {
            // Nothing published anymore, drop whatever was generated for it.
            RemovePackageTasks(packageId);
            return;
        }

        var identifier = new PackageIdentifier { Id = packageId, Version = latest };
        var dllPaths = await _packages.DownloadAllDllsIfMissing(packageId, latest);

        var generated = new List<AutomationTask>();
        foreach (var path in dllPaths)
        {
            string dll = Path.GetFileName(path);
            using TaskLoader loader = new TaskLoader(path);
            foreach (var className in loader.GetClasses())
            {
                var task = BuildCatalogTask(identifier, className, dll);
                TryApplySchema(loader, className, task);
                generated.Add(task);
            }
        }

        lock (_lock)
        {
            // Drop catalog tasks for classes that disappeared in the latest version.
            var keep = generated.Select(x => x.Id).ToHashSet();
            foreach (var id in _catalog.Values
                         .Where(x => x.Target?.Package.Id == packageId && !keep.Contains(x.Id))
                         .Select(x => x.Id)
                         .ToList())
                _catalog.Remove(id);

            foreach (var task in generated)
                _catalog[task.Id] = task;
        }
    }

    public async Task RemovePackageVersionAsync(string packageId, Version version)
    {
        var remaining = (await _packages.GetVersionsAsync(packageId)).ToList();
        if (remaining.Count == 0)
            RemovePackageTasks(packageId);
        else
            // A version was removed; repoint the catalog at the new latest version.
            await SyncPackageAsync(packageId);
    }

    private static AutomationTask BuildCatalogTask(PackageIdentifier identifier, string className, string dll)
    {
        return new AutomationTask
        {
            Id = DeterministicId(identifier.Id, className),
            ParentId = CATALOG_SCOPE_ID,
            Target = new PackageClassTarget(identifier, className) { Dll = dll },
            Metadata = new ScopedMetadata(ShortName(className), EnumScopedType.Task)
            {
                IsReadOnly = true,
                Tags = [PackageTag],
            },
        };
    }

    private static void TryApplySchema(TaskLoader loader, string className, AutomationTask task)
    {
        try
        {
            ITask packageTask = loader.CreateInstance(className);
            task.UpdateFromTask(packageTask);
        }
        catch
        {
            // A single class that fails to instantiate must not break the whole sync.
        }
    }

    private static void RemovePackageTasks(string packageId)
    {
        lock (_lock)
        {
            foreach (var id in _catalog.Values
                         .Where(x => x.Target?.Package.Id == packageId)
                         .Select(x => x.Id)
                         .ToList())
                _catalog.Remove(id);
        }
    }

    /// <summary>
    /// Stable id derived from the package id and class name so repeated syncs upsert the
    /// same task instead of creating duplicates.
    /// </summary>
    private static Guid DeterministicId(string packageId, string className)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes($"{packageId}::{className}"));
        return new Guid(hash);
    }

    private static string ShortName(string className)
    {
        int index = className.LastIndexOf('.');
        return index >= 0 && index < className.Length - 1 ? className[(index + 1)..] : className;
    }
}
