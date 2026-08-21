using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Microsoft.EntityFrameworkCore;

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

    public async Task<ScopedElement> CreateAsync(ScopedElement element)
    {
        element.Id = Guid.NewGuid();

        using var db = _dbContextFactory.CreateDbContext();
        db.ScopedElements.Add(element);
        await db.SaveChangesAsync();

        return element;
    }

    public async Task<ScopedElement> EditAsync(ScopedElement element)
    {
        using var db = _dbContextFactory.CreateDbContext();

        if (!await db.ScopedElements.AnyAsync(x => x.Id == element.Id))
            throw new KeyNotFoundException();

        db.ScopedElements.Update(element);
        await db.SaveChangesAsync();

        return element;
    }

    public async Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.ScopedElements.Where(x => x.ParentId == scopeId).ToListAsync();
    }

    public Task<ScopedElement> RemoveAsync(ScopedElement element)
    {
        throw new NotImplementedException();
    }

    public async Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

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
    }

    public async Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null)
    {
        using var db = _dbContextFactory.CreateDbContext();

        string term = name.ToLower();
        bool exists = await db.ScopedElements.AnyAsync(x =>
            x.ParentId == parentId &&
            x.Id != excludeId &&
            x.Metadata.Name.ToLower() == term);

        return !exists;
    }

    public async Task<List<AutomationTask>> GetTasksByPackageAsync(string packageId)
    {
        using var db = _dbContextFactory.CreateDbContext();

        // Target is stored as an opaque JSON column, so the predicate has to run client-side.
        var tasks = await db.ScopedElements.OfType<AutomationTask>().ToListAsync();
        return tasks.Where(t => t.Target != null && t.Target.Package.Id == packageId).ToList();
    }

    public async Task<Paginated<TaskInstance>> GetHistoryAsync(Guid elementId, PaginationOptions options = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var elements = await db.ScopedElements.AsNoTracking().ToListAsync();

        var byId = elements.ToDictionary(x => x.Id);
        var byParent = elements.ToLookup(x => x.ParentId);

        var taskIds = CollectExecutableIds(elementId, byId, byParent).ToHashSet();
        return await _historyService.SearchAsync(options, taskIds);
    }

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
