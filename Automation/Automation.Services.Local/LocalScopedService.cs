using System.Data;
using Automation.Services.Local.Database;
using Automation.Services.Local.Models;
using Automation.Shared.Base;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Dapper;
using Newtonsoft.Json.Linq;

namespace Automation.Services.Local;

/// <summary>
/// SQLite-backed tree of the scopes and of the tasks and workflows they hold : one table for the
/// tree itself, the graph of a workflow hanging under it in tables of its own. The database takes a
/// whole branch along when the element at its root is removed.
/// </summary>
public class LocalScopedService : IScopedService
{
    /// <summary>
    /// The tasks a graph knows on its own : they are hard coded rather than stored, so nothing
    /// points at them in the scoped elements.
    /// </summary>
    private static readonly Guid[] ControlTaskIds = [.. AutomationControl.All.Select(x => x.Id)];

    private readonly DatabaseFactory _databaseFactory;

    public LocalScopedService(DatabaseFactory databaseFactory)
    {
        _databaseFactory = databaseFactory;
    }

    public async Task<ScopedElement> CreateAsync(ScopedElement element)
    {
        EnsureValidGraph(element);
        element.Id = Guid.NewGuid();

        using var connection = _databaseFactory.Create();
        // The graph of a workflow is written along with it : an element and what hangs under it in
        // other tables land in one go or not at all.
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            ScopedModel.InsertQuery,
            ScopedModel.From(element),
            transaction);

        if (element is AutomationWorkflow workflow)
            await GraphStore.ReplaceAsync(connection, transaction, workflow);

        transaction.Commit();

        return element;
    }

    public async Task<ScopedElement> EditAsync(ScopedElement element)
    {
        EnsureValidGraph(element);

        using var connection = _databaseFactory.Create();
        using var transaction = connection.BeginTransaction();

        // The editor gives back the element as a whole, what it holds included : the row is written
        // again from it, there is nothing to diff against what is stored.
        int updated = await connection.ExecuteAsync($"""
            UPDATE Scoped SET
                {ScopedModel.Assignments}
            WHERE Id = @Id;
            """,
            ScopedModel.From(element),
            transaction);

        if (updated == 0)
            throw new KeyNotFoundException();

        if (element is AutomationWorkflow workflow)
            await GraphStore.ReplaceAsync(connection, transaction, workflow);

        transaction.Commit();

        return element;
    }

    /// <summary>
    /// The element [elementId], null when nothing is stored under that id. A workflow comes with its
    /// graph, ready to be walked.
    /// </summary>
    public async Task<ScopedElement?> GetAsync(Guid elementId)
    {
        return (await GetAsync([elementId])).FirstOrDefault();
    }

    /// <summary>
    /// Refuse a workflow whose graph doesn't hold up (see <see cref="TasksGraph.GetStructureErrors"/>) :
    /// the rule is the storage's, an editor only spares the user from reaching it.
    /// </summary>
    /// <exception cref="InvalidOperationException">The graph is not a workflow the executor can walk.</exception>
    private static void EnsureValidGraph(ScopedElement element)
    {
        if (element is not AutomationWorkflow workflow)
            return;

        List<string> errors = workflow.Graph.GetStructureErrors();

        // The schemas are written from the mappings rather than by hand : a caller reads what a
        // workflow produces without loading its graph, so they are stored along with it.
        errors.AddRange(workflow.DeriveSchemas());

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));
    }

    /// <summary>
    /// The ids of the tasks and workflows the nodes of the workflow [workflowId] point at, as it is
    /// stored. The control tasks are left out : they aren't stored elements, the graph knows them on
    /// its own.
    /// </summary>
    public async Task<List<Guid>> GetGraphTaskIdsAsync(Guid workflowId)
    {
        using var connection = _databaseFactory.Create();

        var ids = await connection.QueryAsync<Guid>("""
            SELECT DISTINCT TaskId FROM GraphNodes
            WHERE WorkflowId = @workflowId AND TaskId IS NOT NULL AND TaskId NOT IN @controlIds;
            """,
            new { workflowId, controlIds = ControlTaskIds });

        return [.. ids];
    }

    /// <summary>
    /// The elements [elementIds], the unknown ones simply left out.
    /// </summary>
    public async Task<List<ScopedElement>> GetAsync(IReadOnlyCollection<Guid> elementIds)
    {
        if (elementIds.Count == 0)
            return [];

        using var connection = _databaseFactory.Create();

        var rows = await connection.QueryAsync<ScopedModel>($"""
            SELECT {ScopedModel.Columns} FROM Scoped WHERE Id IN @elementIds;
            """,
            new { elementIds });

        return await ToElementsAsync(connection, rows);
    }

    public async Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId)
    {
        using var connection = _databaseFactory.Create();

        var rows = await connection.QueryAsync<ScopedModel>($"""
            SELECT {ScopedModel.Columns} FROM Scoped WHERE ParentId = @scopeId;
            """,
            new { scopeId });

        return await ToElementsAsync(connection, rows);
    }

    public async Task<ScopedElement> RemoveAsync(ScopedElement element)
    {
        using var connection = _databaseFactory.Create();

        // The whole branch is read first : what it holds has its say on whether it can go.
        var branch = (await connection.QueryAsync<ScopedModel>($"""
            {ScopedModel.BranchQuery}
            SELECT {ScopedModel.Columns} FROM Scoped WHERE Id IN (SELECT Id FROM Branch);
            """,
            new { elementId = element.Id })).ToList();

        if (branch.Count == 0)
            throw new KeyNotFoundException();

        // Built-in elements (e.g. the control tasks every graph relies on) are read only : neither
        // them nor a scope holding one of them can be removed.
        var protectedElement = branch.FirstOrDefault(x => x.IsReadOnly);
        if (protectedElement != null)
            throw new InvalidOperationException($"The element '{protectedElement.Name}' is read only and can't be removed.");

        // A node points at its task by id, so a task still used by a graph can't be dropped from
        // under it. The workflows being removed along with the element don't count : they are going
        // away with their nodes.
        HashSet<Guid> ids = [.. branch.Select(x => x.Id)];
        var usages = (await GetUsagesAsync(connection, ids)).Where(x => !ids.Contains(x.WorkflowId)).ToList();
        if (usages.Count > 0)
            throw new InvalidOperationException($"Still used by {string.Join(", ", usages.Select(x => x.ToString()))}.");

        // The children of the element, and the history of what the branch ran, all hang under it :
        // the database takes them along.
        await connection.ExecuteAsync("DELETE FROM Scoped WHERE Id = @elementId;", new { elementId = element.Id });

        return element;
    }

    public async Task<List<TaskUsage>> GetUsagesAsync(Guid taskId)
    {
        using var connection = _databaseFactory.Create();
        return await GetUsagesAsync(connection, [taskId]);
    }

    /// <summary>
    /// The nodes pointing at one of [taskIds], with the workflow holding them.
    /// </summary>
    private static async Task<List<TaskUsage>> GetUsagesAsync(IDbConnection connection, IReadOnlyCollection<Guid> taskIds)
    {
        if (taskIds.Count == 0)
            return [];

        var usages = await connection.QueryAsync<TaskUsage>("""
            SELECT
                node.TaskId AS TaskId,
                node.Id AS NodeId,
                node.Name AS NodeName,
                workflow.Id AS WorkflowId,
                workflow.Name AS WorkflowName
            FROM GraphNodes node
            JOIN Scoped workflow ON workflow.Id = node.WorkflowId
            WHERE node.TaskId IN @taskIds;
            """,
            new { taskIds });

        return [.. usages];
    }

    public async Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default)
    {
        using var connection = _databaseFactory.Create();

        // Only the tasks and workflows are searched, a scope is not something that runs.
        const string filter = """
            WHERE ElementKind <> @scopeKind AND (@term = '' OR instr(lower(Name), @term) > 0)
            """;

        using var results = await connection.QueryMultipleAsync($"""
            SELECT COUNT(*) FROM Scoped {filter};

            SELECT {ScopedModel.Columns} FROM Scoped {filter}
            ORDER BY lower(Name)
            LIMIT @take OFFSET @skip;
            """,
            new
            {
                scopeKind = ScopedModel.ScopeKind,
                term = SearchTerm(search),
                take = options.PageSize,
                skip = (options.Page - 1) * options.PageSize,
            });

        long total = await results.ReadSingleAsync<long>();
        var rows = await results.ReadAsync<ScopedModel>();

        return new Paginated<BaseAutomationTask>
        {
            Items = [.. (await ToElementsAsync(connection, rows)).OfType<BaseAutomationTask>()],
            Total = total,
            Options = options,
        };
    }

    public async Task<List<ScopedElement>> SearchTreeAsync(string search = "")
    {
        using var connection = _databaseFactory.Create();

        // Only tasks and workflows are matched, the scopes leading to them coming along so the
        // caller can rebuild the branches from their parents. A scope shared by two results is
        // walked once, which is also what keeps the walk from turning on itself.
        var rows = await connection.QueryAsync<ScopedModel>($"""
            WITH RECURSIVE Matched(Id, ParentId) AS (
                SELECT Id, ParentId FROM Scoped
                WHERE ElementKind <> @scopeKind AND (@term = '' OR instr(lower(Name), @term) > 0)
            ),
            Tree(Id, ParentId) AS (
                SELECT Id, ParentId FROM Matched
                UNION
                SELECT parent.Id, parent.ParentId FROM Scoped parent JOIN Tree ON Tree.ParentId = parent.Id
            )
            SELECT {ScopedModel.Columns} FROM Scoped WHERE Id IN (SELECT Id FROM Tree);
            """,
            new { scopeKind = ScopedModel.ScopeKind, term = SearchTerm(search) });

        return await ToElementsAsync(connection, rows);
    }

    public async Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null)
    {
        using var connection = _databaseFactory.Create();

        // IS NOT rather than <> : an element keeping its own name is left out of the search, and
        // nothing is when no element is excluded.
        return await connection.ExecuteScalarAsync<bool>("""
            SELECT NOT EXISTS(
                SELECT 1 FROM Scoped
                WHERE ParentId = @parentId AND Id IS NOT @excludeId AND lower(Name) = @term
            );
            """,
            new { parentId, excludeId, term = name.ToLower() });
    }

    public async Task<JObject> GetContextAsync(Guid elementId)
    {
        using var connection = _databaseFactory.Create();

        // The scopes from the root down to the one holding the element, the resolution going back
        // down so a scope overrides its parents. The element itself is walked over unless it is a
        // scope, and an unknown element simply leads to no scope at all.
        var rows = await connection.QueryAsync<ScopedModel>($"""
            WITH RECURSIVE Ancestry(AncestorId, AncestorParentId, Depth) AS (
                SELECT Id, ParentId, 0 FROM Scoped WHERE Id = @elementId
                UNION ALL
                SELECT parent.Id, parent.ParentId, Ancestry.Depth + 1
                FROM Scoped parent JOIN Ancestry ON Ancestry.AncestorParentId = parent.Id
            )
            SELECT {ScopedModel.Columns} FROM Scoped
            JOIN Ancestry ON AncestorId = Scoped.Id
            WHERE ElementKind = @scopeKind
            ORDER BY Depth DESC;
            """,
            new { elementId, scopeKind = ScopedModel.ScopeKind });

        return ScopeContextResolver.Resolve(rows.Select(x => x.ToElement()).OfType<Scope>());
    }

    /// <summary>
    /// What a search is matched on : nothing when it holds nothing to look for.
    /// </summary>
    private static string SearchTerm(string search)
    {
        return string.IsNullOrWhiteSpace(search) ? "" : search.ToLower();
    }

    /// <summary>
    /// The elements [rows] stand for, the workflows of them coming with their graph : it lives in
    /// tables of its own, read once for all the workflows of the read rather than one by one.
    /// </summary>
    private static async Task<List<ScopedElement>> ToElementsAsync(IDbConnection connection, IEnumerable<ScopedModel> rows)
    {
        List<ScopedElement> elements = [.. rows.Select(x => x.ToElement())];

        List<AutomationWorkflow> workflows = [.. elements.OfType<AutomationWorkflow>()];
        if (workflows.Count == 0)
            return elements;

        var graphs = await GraphStore.LoadAsync(connection, [.. workflows.Select(x => x.Id)]);
        foreach (AutomationWorkflow workflow in workflows)
            workflow.Graph = graphs[workflow.Id];

        return elements;
    }
}
