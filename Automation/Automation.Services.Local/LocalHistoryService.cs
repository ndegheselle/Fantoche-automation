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

    /// <summary>
    /// The branches of a workflow run in parallel and all report through here : the writes are
    /// serialized so two instances of the same run never race on the same context.
    /// </summary>
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public event Action<TaskInstance>? InstanceAdded;
    public event Action<TaskInstance>? InstanceUpdated;

    public LocalHistoryService(LocalDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Paginated<TaskInstance>> GetByScopedAsync(Guid elementId, PaginationOptions options = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var elements = await db.ScopedElements.AsNoTracking().ToListAsync();

        var byId = elements.ToDictionary(x => x.Id);
        var byParent = elements.ToLookup(x => x.ParentId);

        var taskIds = LocalScopedService.CollectExecutableIds(elementId, byId, byParent).ToHashSet();
        return await GetAsync(taskIds, options);
    }

    public async Task<IReadOnlyList<TaskInstance>> GetChildrenAsync(Guid instanceId)
    {
        using var db = _dbContextFactory.CreateDbContext();

        return await db.TaskInstances
            .AsNoTracking()
            .Where(x => x.ParentInstanceId == instanceId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    private async Task<Paginated<TaskInstance>> GetAsync(
        IReadOnlyCollection<Guid>? taskIds = null,
        PaginationOptions options = default)
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

    /// <summary>
    /// Persist [instance] : added the first time it is seen, updated afterwards. Only the stored
    /// data is written, the runtime data of the instance (graph node, parent workflow, ...) is not.
    /// </summary>
    public async Task SaveAsync(TaskInstance instance)
    {
        bool added;
        await _writeLock.WaitAsync();
        try
        {
            using var db = _dbContextFactory.CreateDbContext();

            var stored = await db.TaskInstances.FirstOrDefaultAsync(x => x.Id == instance.Id);
            added = stored == null;
            if (stored == null)
            {
                // A WorkflowInstance carries the runtime of a run and can't be materialized by EF :
                // whatever is executed, what is stored is always a plain instance.
                stored = new TaskInstance() { Id = instance.Id };
                db.TaskInstances.Add(stored);
            }

            stored.TaskId = instance.TaskId;
            stored.NodeId = instance.NodeId;
            stored.ParentInstanceId = instance.ParentInstanceId;
            stored.NodeName = instance.NodeName;
            stored.Parameters = instance.Parameters;
            stored.Output = instance.Output;
            stored.CreatedAt = instance.CreatedAt;
            stored.State = instance.State;
            // Assigning the state stamps FinishedAt, the stored value has to stay the one of the run.
            stored.FinishedAt = instance.FinishedAt;

            await db.SaveChangesAsync();
        }
        finally
        {
            _writeLock.Release();
        }

        if (added)
            InstanceAdded?.Invoke(instance);
        else
            InstanceUpdated?.Invoke(instance);
    }

    public async Task RemoveAsync(IReadOnlyCollection<Guid> taskIds)
    {
        if (taskIds.Count == 0)
            return;

        using var db = _dbContextFactory.CreateDbContext();
        await db.TaskInstances.Where(x => taskIds.Contains(x.TaskId)).ExecuteDeleteAsync();
    }
}
