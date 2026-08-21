using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace Automation.Services.Local;

/// <summary>
/// SQLite-backed history of task instances.
/// </summary>
public class LocalHistoryService : IHistoryService
{
    private readonly LocalDbContextFactory _dbContextFactory;

    public event Action<TaskInstance>? InstanceAdded;
    public event Action<TaskInstance>? InstanceUpdated;

    public LocalHistoryService(LocalDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Paginated<TaskInstance>> SearchAsync(
        PaginationOptions options = default,
        IReadOnlyCollection<Guid>? taskIds = null)
    {
        using var db = _dbContextFactory.CreateDbContext();

        IQueryable<TaskInstance> query = db.TaskInstances;
        if (taskIds != null)
            query = query.Where(x => taskIds.Contains(x.TaskId));

        query = query.OrderByDescending(x => x.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync();

        return new Paginated<TaskInstance>
        {
            Items = items,
            Total = total,
            Options = options,
        };
    }
}
