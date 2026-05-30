using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.App.Services.Abstractions
{
    /// <summary>
    /// Package registry: search, versions, contained task targets, upload/remove. See
    /// <see cref="IScopesService"/> for the architecture rationale.
    /// NOTE: the old PackagesClient.GetClassesAsync returned the removed `ClassIdentifier`; the
    /// current equivalent is <see cref="TaskTarget"/> (ClassTarget / PackageClassTarget).
    /// </summary>
    public interface IPackagesService
    {
        Task<ListPageWrapper<PackageInfos>> SearchAsync(string searchValue, int page, int pageSize);

        Task<PackageInfos?> GetByIdAsync(string id);

        Task<IReadOnlyList<Version>> GetVersionsAsync(string id);

        /// <summary>Task targets (classes) contained in a given package version.</summary>
        Task<IReadOnlyList<TaskTarget>> GetClassesAsync(string id, Version version);

        Task<PackageInfos> CreateAsync(string filePath);

        Task<PackageInfos> CreateVersionAsync(string id, string filePath);

        Task RemoveVersionAsync(string id, Version version);
    }
}
