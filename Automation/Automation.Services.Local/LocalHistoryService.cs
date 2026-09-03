using Automation.Services.Local.Database;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Dapper;

namespace Automation.Services.Local;

/// <summary>
/// SQLite-backed history of task instances. Nothing removes them : an instance hangs under the
/// task it ran and, for the node of a run, under the instance of the run itself, so removing a
/// task takes its whole history along.
/// </summary>
public class LocalHistoryService : IHistoryService
{
    private readonly DatabaseFactory _databaseFactory;

    /// <summary>
    /// The branches of a workflow run in parallel and all report through here : the writes are
    /// serialized so two instances of the same run never race on the same row.
    /// </summary>
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public event Action<TaskInstance>? InstanceAdded;
    public event Action<TaskInstance>? InstanceUpdated;

    public LocalHistoryService(DatabaseFactory databaseFactory)
    {
        _databaseFactory = databaseFactory;
    }

    /// <summary>
    /// The branch of the scoped tree hanging under @elementId, the element itself included. Only
    /// the tasks and workflows of it ever have instances, so the scopes coming along in the walk
    /// don't have to be told apart : they simply match nothing.
    /// </summary>
    private const string BranchQuery = """
        WITH RECURSIVE Branch(Id) AS (
            SELECT Id FROM Scoped WHERE Id = @elementId
            UNION ALL
            SELECT child.Id FROM Scoped child JOIN Branch ON child.ParentId = Branch.Id
        )
        """;

    public async Task<Paginated<TaskInstance>> GetByScopedAsync(Guid elementId, PaginationOptions options = default)
    {
        using var connection = _databaseFactory.Create();

        // The count and the page are asked for in one round trip, each statement carrying the walk
        // of the tree : a common table expression only lives for the statement it is written on.
        using var results = await connection.QueryMultipleAsync($"""
            {BranchQuery}
            SELECT COUNT(*) FROM TaskInstances WHERE TaskId IN (SELECT Id FROM Branch);

            {BranchQuery}
            SELECT {TaskInstanceModel.Columns} FROM TaskInstances
            WHERE TaskId IN (SELECT Id FROM Branch)
            ORDER BY CreatedAt DESC
            LIMIT @take OFFSET @skip;
            """,
            new
            {
                elementId,
                take = options.PageSize,
                skip = (options.Page - 1) * options.PageSize,
            });

        long total = await results.ReadSingleAsync<long>();
        var rows = await results.ReadAsync<TaskInstanceModel>();

        return new Paginated<TaskInstance>
        {
            Items = rows.Select(x => x.ToInstance()).ToList(),
            Total = total,
            Options = options,
        };
    }

    public async Task<IReadOnlyList<TaskInstance>> GetChildrenAsync(Guid instanceId)
    {
        using var connection = _databaseFactory.Create();

        var rows = await connection.QueryAsync<TaskInstanceModel>($"""
            SELECT {TaskInstanceModel.Columns} FROM TaskInstances
            WHERE ParentInstanceId = @instanceId
            ORDER BY CreatedAt;
            """,
            new { instanceId });

        return [.. rows.Select(x => x.ToInstance())];
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
            using var connection = _databaseFactory.Create();

            // The row is written blind and the conflict on its id tells whether the instance was
            // already known : nothing else is let through, a missing task or column stays a failure.
            added = await connection.ExecuteAsync($"""
                INSERT INTO TaskInstances ({TaskInstanceModel.Columns})
                VALUES (@Id, @TaskId, @NodeId, @ParentInstanceId, @NodeName, @Parameters, @Output, @State, @CreatedAt, @FinishedAt)
                ON CONFLICT (Id) DO NOTHING;
                """,
                instance) > 0;

            if (!added)
            {
                await connection.ExecuteAsync("""
                    UPDATE TaskInstances SET
                        TaskId = @TaskId,
                        NodeId = @NodeId,
                        ParentInstanceId = @ParentInstanceId,
                        NodeName = @NodeName,
                        Parameters = @Parameters,
                        Output = @Output,
                        State = @State,
                        CreatedAt = @CreatedAt,
                        FinishedAt = @FinishedAt
                    WHERE Id = @Id;
                    """,
                    TaskInstanceModel.From(instance));
            }
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
