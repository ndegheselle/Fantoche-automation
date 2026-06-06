using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.Shared.Services;

public interface IPackagesService
{
    public Task<Paginated<PackageInfos>> SearchAsync(string search = "", PaginationOptions options = default);
    public Task RemoveAsync(string id, Version version);
}