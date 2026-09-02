using Automation.Services.Local.Database;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace Automation.Services.Local;

/// <summary>
/// SQLite-backed history of task instances. Nothing removes them : an instance hangs under the
/// task it ran and, for the node of a run, under the instance of the run itself, so removing a
/// task takes its whole history along.
/// </summary>
public class LocalHistoryService : IHistoryService
{
    private readonly SqliteContextFactory _contextFactory;

    /// <summary>
    /// The branches of a workflow run in parallel and all report through here : the writes are
    /// serialized so two instances of the same run never race on the same context.
    /// </summary>
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public event Action<TaskInstance>? InstanceAdded;
    public event Action<TaskInstance>? InstanceUpdated;

    public LocalHistoryService(SqliteContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Paginated<TaskInstance>> GetByScopedAsync(Guid elementId, PaginationOptions options = default)
    {
        using var db = _contextFactory.CreateContext();

        // Only the shape of the tree is needed to know which tasks the history is asked for, not
        // what the elements hold (a workflow would come with its whole graph).
        var elements = await db.Scoped
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Select(x => new LocalScopedService.ScopedLink(x.Id, x.ParentId, x is BaseAutomationTask))
            .ToListAsync();

        var byId = elements.ToDictionary(x => x.Id);
        var byParent = elements.ToLookup(x => x.ParentId);

        var taskIds = LocalScopedService.CollectExecutableIds(elementId, byId, byParent).ToHashSet();
        return await GetAsync(taskIds, options);
    }

    public async Task<IReadOnlyList<TaskInstance>> GetChildrenAsync(Guid instanceId)
    {
        using var db = _contextFactory.CreateContext();

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
        using var db = _contextFactory.CreateContext();

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
            using var db = _contextFactory.CreateContext();

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
}
