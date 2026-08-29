using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Automation.Services.Local;

public class LocalScopedService : IScopedService
{
    private readonly IHistoryService _historyService;
    private readonly LocalDbContextFactory _dbContextFactory;

    public LocalScopedService(IHistoryService historyService, LocalDbContextFactory dbContextFactory)
    {
        _historyService = historyService;
        _dbContextFactory = dbContextFactory;
    }

    public Task<ScopedElement> CreateAsync(ScopedElement element) => _dbContextFactory.QueryAsync(async db =>
    {
        element.Id = Guid.NewGuid();

        db.ScopedElements.Add(element);
        await db.SaveChangesAsync();

        return element;
    });

    public Task<ScopedElement> EditAsync(ScopedElement element) => _dbContextFactory.QueryAsync(async db =>
    {
        if (!await db.ScopedElements.AnyAsync(x => x.Id == element.Id))
            throw new KeyNotFoundException();

        db.ScopedElements.Update(element);
        await db.SaveChangesAsync();

        return element;
    });

    public Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId) => _dbContextFactory.QueryAsync(
        db => db.ScopedElements.Where(x => x.ParentId == scopeId).ToListAsync());

    public Task<ScopedElement> RemoveAsync(ScopedElement element)
    {
        throw new NotImplementedException();
    }

    public Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default)
        => _dbContextFactory.QueryAsync(async db =>
    {
        IQueryable<BaseAutomationTask> query = db.ScopedElements.OfType<BaseAutomationTask>();
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
    });

    public Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null)
        => _dbContextFactory.QueryAsync(async db =>
    {
        string term = name.ToLower();
        bool exists = await db.ScopedElements.AnyAsync(x =>
            x.ParentId == parentId &&
            x.Id != excludeId &&
            x.Metadata.Name.ToLower() == term);

        return !exists;
    });

    public Task<List<AutomationTask>> GetTasksByPackageAsync(string packageId)
        => _dbContextFactory.QueryAsync(async db =>
    {
        // Target is stored as an opaque JSON column, so the predicate has to run client-side.
        var tasks = await db.ScopedElements.OfType<AutomationTask>().ToListAsync();
        return tasks.Where(t => t.Target != null && t.Target.Package.Id == packageId).ToList();
    });

    public Task<JObject> GetContextAsync(Guid elementId) => _dbContextFactory.QueryAsync(async db =>
    {
        var element = await db.ScopedElements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == elementId);
        if (element == null)
            return new JObject();

        var scopes = await db.ScopedElements.AsNoTracking().OfType<Scope>().ToDictionaryAsync(x => x.Id);

        // Walk up to the root, the resolution then going back down so a scope overrides its parents.
        List<Scope> hierarchy = [];
        Guid? currentId = element is Scope ? element.Id : element.ParentId;
        while (currentId != null && scopes.TryGetValue(currentId.Value, out var scope))
        {
            hierarchy.Insert(0, scope);
            currentId = scope.ParentId;
        }

        return ScopeContextResolver.Resolve(hierarchy);
    });

    public Task<Paginated<TaskInstance>> GetHistoryAsync(Guid elementId, PaginationOptions options = default)
        => _dbContextFactory.QueryAsync(async db =>
    {
        var elements = await db.ScopedElements.AsNoTracking().ToListAsync();

        var byId = elements.ToDictionary(x => x.Id);
        var byParent = elements.ToLookup(x => x.ParentId);

        var taskIds = CollectExecutableIds(elementId, byId, byParent).ToHashSet();
        return await _historyService.SearchAsync(options, taskIds);
    });

    /// <summary>
    /// Collect the ids whose executions make up [elementId]'s history: the element itself when it is a
    /// task or workflow, or every task/workflow nested under it (recursively) when it is a scope.
    /// </summary>
    private static IEnumerable<Guid> CollectExecutableIds(
        Guid elementId,
        Dictionary<Guid, ScopedElement> byId,
        ILookup<Guid?, ScopedElement> byParent)
    {
        if (!byId.TryGetValue(elementId, out var element))
            yield break;

        switch (element)
        {
            case BaseAutomationTask:
                // Tasks and workflows are the executable leaves.
                yield return element.Id;
                break;
            case Scope:
                foreach (var child in byParent[elementId])
                    foreach (var id in CollectExecutableIds(child.Id, byId, byParent))
                        yield return id;
                break;
        }
    }
}
