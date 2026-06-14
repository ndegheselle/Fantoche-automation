using Automation.Shared.Data.Scoped;

namespace Automation.Shared.Services;

/// <summary>
/// Maintains the read-only "catalog" tasks that mirror the task classes exposed by a
/// package. One <see cref="AutomationTask"/> is generated per package class and kept
/// pointing at the latest available version of its package. These tasks are read-only:
/// users who need a pinned/customized version create their own task targeting a
/// specific version (see <see cref="IScopedService.CreateAsync"/>).
/// </summary>
public interface ITasksService
{
    /// <summary>
    /// Get the read-only catalog tasks generated for a package (latest version).
    /// Pass an empty id to get the catalog tasks of every package.
    /// </summary>
    public Task<List<AutomationTask>> GetPackageTasksAsync(string packageId = "");

    /// <summary>
    /// Upsert the read-only catalog tasks for a package, repointing them at the latest
    /// available version and refreshing their input/output schemas. Call this when a
    /// package is added or when a new version is published. Catalog tasks for classes
    /// that no longer exist in the latest version are removed.
    /// This is the single place that loads the package assemblies, so callers that just
    /// added a package should rely on the returned tasks instead of loading it again
    /// (e.g. an empty result means the package exposes no compatible task).
    /// </summary>
    public Task<List<AutomationTask>> SyncPackageAsync(string packageId);

    /// <summary>
    /// Reconcile the catalog after a version was removed. If no version remains the
    /// catalog tasks of the package are removed, otherwise they are repointed at the
    /// new latest version.
    /// </summary>
    public Task RemovePackageVersionAsync(string packageId, Version version);
}
