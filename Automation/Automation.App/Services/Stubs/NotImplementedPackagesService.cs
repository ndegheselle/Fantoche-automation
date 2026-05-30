using Automation.App.Services.Abstractions;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.App.Services.Stubs
{
    /// <summary>Placeholder pending the worker+SQLite rework. See NotImplementedScopesService.</summary>
    public class NotImplementedPackagesService : IPackagesService
    {
        private const string Pending = "Packages data source not implemented yet (pending worker+SQLite rework).";

        public Task<ListPageWrapper<PackageInfos>> SearchAsync(string searchValue, int page, int pageSize) => throw new NotImplementedException(Pending);

        public Task<PackageInfos?> GetByIdAsync(string id) => throw new NotImplementedException(Pending);

        public Task<IReadOnlyList<Version>> GetVersionsAsync(string id) => throw new NotImplementedException(Pending);

        public Task<IReadOnlyList<TaskTarget>> GetClassesAsync(string id, Version version) => throw new NotImplementedException(Pending);

        public Task<PackageInfos> CreateAsync(string filePath) => throw new NotImplementedException(Pending);

        public Task<PackageInfos> CreateVersionAsync(string id, string filePath) => throw new NotImplementedException(Pending);

        public Task RemoveVersionAsync(string id, Version version) => throw new NotImplementedException(Pending);
    }
}
