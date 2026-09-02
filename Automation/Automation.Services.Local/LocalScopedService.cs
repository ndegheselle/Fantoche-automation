using Automation.Shared.Base;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Automation.Services.Local;

public class LocalScopedService : IScopedService
{
    private readonly SqliteContextFactory _contextFactory;

    public LocalScopedService(SqliteContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ScopedElement> CreateAsync(ScopedElement element)
    {
        element.Id = Guid.NewGuid();

        using var db = _contextFactory.CreateContext();
        db.Scoped.Add(element);
        await db.SaveChangesAsync();

        return element;
    }

    public async Task<ScopedElement> EditAsync(ScopedElement element)
    {
        using var db = _contextFactory.CreateContext();

        if (!await db.Scoped.AnyAsync(x => x.Id == element.Id))
            throw new KeyNotFoundException();

        // What hangs under the element in tables of its own — its schedules, and the whole graph of
        // a workflow — is dropped and written again rather than diffed against what is stored : the
        // editor gives back the element as a whole, holding the very ids that are stored, so
        // rewriting the rows lands on the same ones a diff would have.
        await ClearDependentRowsAsync(db, element);

        // Update() would mark everything it reaches as modified, which the rows just dropped are
        // not anymore : only what shares the table of the element itself (its metadata, its
        // settings, its target) is an update, the rest is an insert.
        string? table = db.Model.FindEntityType(element.GetType())?.GetTableName();
        db.ChangeTracker.TrackGraph(element, node => node.Entry.State =
            node.Entry.Metadata.GetTableName() == table ? EntityState.Modified : EntityState.Added);

        await db.SaveChangesAsync();

        return element;
    }

    /// <summary>
    /// Drop the rows [element] owns in other tables. Removing the graph of a workflow takes its
    /// nodes with it, and each node its connectors and their connections.
    /// </summary>
    private static async Task ClearDependentRowsAsync(DatabaseContext db, ScopedElement element)
    {
        if (element is BaseAutomationTask)
        {
            await db.Schedules
                .Where(x => EF.Property<Guid>(x, DatabaseContext.ScheduleTaskId) == element.Id)
                .ExecuteDeleteAsync();
        }

        if (element is AutomationWorkflow)
        {
            await db.Graphs
                .Where(x => EF.Property<Guid>(x, DatabaseContext.GraphWorkflowId) == element.Id)
                .ExecuteDeleteAsync();
        }
    }

    public async Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId)
    {
        using var db = _contextFactory.CreateContext();
        return await db.Scoped.Where(x => x.ParentId == scopeId).ToListAsync();
    }

    public async Task<ScopedElement> RemoveAsync(ScopedElement element)
    {
        using var db = _contextFactory.CreateContext();

        if (!await db.Scoped.AnyAsync(x => x.Id == element.Id))
            throw new KeyNotFoundException();

        HashSet<Guid> ids = await CollectBranchAsync(db, element.Id);

        var removed = await db.Scoped.Where(x => ids.Contains(x.Id)).ToListAsync();

        // Built-in elements (e.g. the control tasks every graph relies on) are read only : neither
        // them nor a scope holding one of them can be removed.
        var protectedElement = removed.FirstOrDefault(x => x.Metadata.IsReadOnly);
        if (protectedElement != null)
            throw new InvalidOperationException($"The element '{protectedElement.Metadata.Name}' is read only and can't be removed.");

        // A node points at its task through a relation, so a task still used by a graph can't be
        // dropped from under it. The graphs being removed along with the element don't count : they
        // are going away with their nodes.
        var usages = (await GetUsagesAsync(db, ids)).Where(x => !ids.Contains(x.WorkflowId)).ToList();
        if (usages.Count > 0)
            throw new InvalidOperationException($"Still used by {string.Join(", ", usages.Select(x => x.ToString()))}.");

        // The children of the removed elements, the graph of a workflow and the history of what
        // they ran all hang under them : the database takes them along.
        db.Scoped.RemoveRange(removed);
        await db.SaveChangesAsync();

        return element;
    }

    /// <summary>
    /// [elementId] and everything under it, read one level at a time : a whole branch of the tree.
    /// </summary>
    private static async Task<HashSet<Guid>> CollectBranchAsync(DatabaseContext db, Guid elementId)
    {
        HashSet<Guid> ids = [elementId];

        List<Guid> level = [elementId];
        while (level.Count > 0)
        {
            level = await db.Scoped
                .AsNoTracking()
                .Where(x => x.ParentId != null && level.Contains(x.ParentId.Value))
                .Select(x => x.Id)
                .ToListAsync();
            level = [.. level.Where(ids.Add)];
        }

        return ids;
    }

    public async Task<List<TaskUsage>> GetUsagesAsync(Guid taskId)
    {
        using var db = _contextFactory.CreateContext();
        return await GetUsagesAsync(db, [taskId]);
    }

    /// <summary>
    /// The nodes pointing at one of [taskIds], with the workflow holding them.
    /// </summary>
    private static async Task<List<TaskUsage>> GetUsagesAsync(DatabaseContext db, IReadOnlyCollection<Guid> taskIds)
    {
        if (taskIds.Count == 0)
            return [];

        return await (
            from node in db.GraphNodes.AsNoTracking().OfType<BaseGraphTask>()
            join workflow in db.Scoped.AsNoTracking().OfType<AutomationWorkflow>()
                on EF.Property<Guid>(node, DatabaseContext.NodeGraphId) equals workflow.Id
            where taskIds.Contains(node.TaskId)
            select new TaskUsage()
            {
                TaskId = node.TaskId,
                WorkflowId = workflow.Id,
                WorkflowName = workflow.Metadata.Name,
                NodeId = node.Id,
                NodeName = node.Metadata.Name,
            }).ToListAsync();
    }

    public async Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default)
    {
        using var db = _contextFactory.CreateContext();

        IQueryable<BaseAutomationTask> query = db.Scoped.AsNoTracking().OfType<BaseAutomationTask>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.ToLower();
            query = query.Where(x => x.Metadata.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync();

        return new Paginated<BaseAutomationTask>
        {
            Items = items,
            Total = total,
            Options = options,
        };
    }

    public async Task<List<ScopedElement>> SearchTreeAsync(string search = "")
    {
        using var db = _contextFactory.CreateContext();

        // Only tasks and workflows are matched, scopes being kept only for the branches they hold.
        IQueryable<BaseAutomationTask> query = db.Scoped.AsNoTracking().OfType<BaseAutomationTask>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.ToLower();
            query = query.Where(x => x.Metadata.Name.ToLower().Contains(term));
        }

        List<ScopedElement> results = [.. await query.ToListAsync()];
        HashSet<Guid> known = [.. results.Select(x => x.Id)];

        // Every result comes with the scopes leading to it, so the caller can rebuild the branches
        // from ParentId, reading them one level at a time.
        List<Guid> missing = [.. results.Select(x => x.ParentId).OfType<Guid>().Where(known.Add)];
        while (missing.Count > 0)
        {
            var parents = await db.Scoped
                .AsNoTracking()
                .Where(x => missing.Contains(x.Id))
                .ToListAsync();

            results.AddRange(parents);
            missing = [.. parents.Select(x => x.ParentId).OfType<Guid>().Where(known.Add)];
        }

        return results;
    }

    public async Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null)
    {
        using var db = _contextFactory.CreateContext();

        string term = name.ToLower();
        bool exists = await db.Scoped.AnyAsync(x =>
            x.ParentId == parentId &&
            x.Id != excludeId &&
            x.Metadata.Name.ToLower() == term);

        return !exists;
    }

    public async Task<JObject> GetContextAsync(Guid elementId)
    {
        using var db = _contextFactory.CreateContext();

        // Only the place of the element in the tree matters here, not what it holds.
        var element = await db.Scoped
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Select(x => new { x.Id, x.ParentId, IsScope = x is Scope })
            .FirstOrDefaultAsync(x => x.Id == elementId);
        if (element == null)
            return new JObject();

        var scopes = await db.Scoped.AsNoTracking().OfType<Scope>().ToDictionaryAsync(x => x.Id);

        // Walk up to the root, the resolution then going back down so a scope overrides its parents.
        List<Scope> hierarchy = [];
        Guid? currentId = element.IsScope ? element.Id : element.ParentId;
        while (currentId != null && scopes.TryGetValue(currentId.Value, out var scope))
        {
            hierarchy.Insert(0, scope);
            currentId = scope.ParentId;
        }

        return ScopeContextResolver.Resolve(hierarchy);
    }

    /// <summary>
    /// The place of a scoped element in the tree, and whether it is one of the executable leaves.
    /// Read on its own so that walking the tree doesn't have to load what the elements hold.
    /// </summary>
    public record ScopedLink(Guid Id, Guid? ParentId, bool IsExecutable);

    /// <summary>
    /// Collect the ids whose executions make up [elementId]'s history: the element itself when it is a
    /// task or workflow, or every task/workflow nested under it (recursively) when it is a scope.
    /// </summary>
    public static IEnumerable<Guid> CollectExecutableIds(
        Guid elementId,
        Dictionary<Guid, ScopedLink> byId,
        ILookup<Guid?, ScopedLink> byParent)
    {
        if (!byId.TryGetValue(elementId, out var element))
            yield break;

        // Tasks and workflows are the executable leaves, a scope only holds them.
        if (element.IsExecutable)
        {
            yield return element.Id;
            yield break;
        }

        foreach (var child in byParent[elementId])
            foreach (var id in CollectExecutableIds(child.Id, byId, byParent))
                yield return id;
    }
}
