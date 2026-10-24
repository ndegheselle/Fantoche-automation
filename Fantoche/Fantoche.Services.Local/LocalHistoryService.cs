using Fantoche.Services.Local.Database;
using Fantoche.Services.Local.Models;
using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using Dapper;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Local;

/// <summary>
/// SQLite-backed history of task instances. An instance hangs under the task it ran and, for the
/// node of a run, under the instance of the run itself, so removing a task or a run takes its whole
/// history along. The supervisor removes the executions older than a month (see
/// <see cref="LocalSupervisor.CleanUpAsync"/>).
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
    /// The branch of the scoped tree the history is asked for. Only the tasks and workflows of it
    /// ever have instances, so the scopes coming along in the walk don't have to be told apart :
    /// they simply match nothing.
    /// </summary>
    private const string BranchQuery = ScopedModel.BranchQuery;

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

    /// <summary>
    /// The instance [instanceId] as it was last reported, null when nothing ran under that id.
    /// </summary>
    public async Task<TaskInstance?> GetAsync(Guid instanceId)
    {
        using var connection = _databaseFactory.Create();

        var row = await connection.QuerySingleOrDefaultAsync<TaskInstanceModel>($"""
            SELECT {TaskInstanceModel.Columns} FROM TaskInstances WHERE Id = @instanceId;
            """,
            new { instanceId });

        return row?.ToInstance();
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

    public async Task<IReadOnlyList<TaskInstance>> GetDescendantsAsync(Guid instanceId)
    {
        using var connection = _databaseFactory.Create();

        var rows = await connection.QueryAsync<TaskInstanceModel>($"""
            WITH RECURSIVE Descendants(Id) AS (
                SELECT Id FROM TaskInstances WHERE ParentInstanceId = @instanceId
                UNION ALL
                SELECT child.Id FROM TaskInstances child JOIN Descendants ON child.ParentInstanceId = Descendants.Id
            )
            SELECT {TaskInstanceModel.Columns} FROM TaskInstances
            WHERE Id IN (SELECT Id FROM Descendants)
            ORDER BY CreatedAt;
            """,
            new { instanceId });

        return [.. rows.Select(x => x.ToInstance())];
    }

    public async Task<IReadOnlyList<TaskInstance>> GetUnfinishedRunsAsync(string? applicationId)
    {
        using var connection = _databaseFactory.Create();

        var rows = await connection.QueryAsync<TaskInstanceModel>($"""
            SELECT {TaskInstanceModel.Columns} FROM TaskInstances
            WHERE ParentInstanceId IS NULL AND ApplicationId IS @applicationId AND (State & @finished) = 0;
            """,
            new { applicationId, finished = (int)EnumTaskState.Finished });

        return [.. rows.Select(x => x.ToInstance())];
    }

    public async Task<bool> ClaimRunAsync(Guid runId, string sessionId)
    {
        int claimed;
        await _writeLock.WaitAsync();
        try
        {
            using var connection = _databaseFactory.Create();
            // In one statement : another process resuming it at the same time reads it the same.
            claimed = await connection.ExecuteAsync("""
                UPDATE TaskInstances SET State = @progressing, SessionId = @sessionId
                WHERE Id = @runId AND State IN (@interrupted, @paused, @awaitingUser);
                """,
                new
                {
                    runId,
                    sessionId,
                    progressing = (int)EnumTaskState.Progressing,
                    interrupted = (int)EnumTaskState.Interrupted,
                    paused = (int)EnumTaskState.Paused,
                    awaitingUser = (int)EnumTaskState.AwaitingUser,
                });
        }
        finally
        {
            _writeLock.Release();
        }

        if (claimed == 0)
            return false;

        TaskInstance? run = await GetAsync(runId);
        if (run != null)
            InstanceUpdated?.Invoke(run);
        return true;
    }

    public async Task<bool> AnswerAsync(Guid instanceId, JToken answer)
    {
        int answered;
        await _writeLock.WaitAsync();
        try
        {
            using var connection = _databaseFactory.Create();
            // In one statement : two users answering at once both read it waiting.
            answered = await connection.ExecuteAsync("""
                UPDATE TaskInstances SET State = @completed, Output = @answer, FinishedAt = @now
                WHERE Id = @instanceId AND State = @awaitingUser;
                """,
                new
                {
                    instanceId,
                    answer = answer.ToString(Newtonsoft.Json.Formatting.None),
                    now = DateTime.UtcNow,
                    completed = (int)EnumTaskState.Completed,
                    awaitingUser = (int)EnumTaskState.AwaitingUser,
                });
        }
        finally
        {
            _writeLock.Release();
        }

        if (answered == 0)
            return false;

        TaskInstance? instance = await GetAsync(instanceId);
        if (instance != null)
            InstanceUpdated?.Invoke(instance);
        return true;
    }

    public async Task InterruptRunAsync(Guid runId)
    {
        TaskInstance? run = await GetAsync(runId);
        if (run == null || (run.State & EnumTaskState.Finished) != 0)
            return;

        await InterruptAsync(run);
    }

    private async Task InterruptAsync(TaskInstance run)
    {
        IReadOnlyList<TaskInstance> descendants = await GetDescendantsAsync(run.Id);
        IEnumerable<TaskInstance> running = descendants.Where(x => x.State is EnumTaskState.Pending or EnumTaskState.Progressing);
        foreach (TaskInstance instance in running)
        {
            instance.State = EnumTaskState.Interrupted;
            await SaveAsync(instance);
        }

        run.State = EnumTaskState.Interrupted;
        await SaveAsync(run);
    }

    public async Task<IReadOnlyList<Guid>> GetRunIdsAsync(string? applicationId)
    {
        using var connection = _databaseFactory.Create();
        var ids = await connection.QueryAsync<Guid>("""
            SELECT Id FROM TaskInstances WHERE ParentInstanceId IS NULL AND ApplicationId IS @applicationId;
            """,
            new { applicationId });
        return [.. ids];
    }

    /// <summary>The executions started before [cutoff] : the runs, the instances they ran left out.</summary>
    public async Task<IReadOnlyList<TaskInstance>> GetRunsStartedBeforeAsync(DateTime cutoff)
    {
        using var connection = _databaseFactory.Create();
        var rows = await connection.QueryAsync<TaskInstanceModel>($"""
            SELECT {TaskInstanceModel.Columns} FROM TaskInstances
            WHERE ParentInstanceId IS NULL AND CreatedAt < @cutoff;
            """,
            new { cutoff = cutoff.ToUniversalTime() });
        return [.. rows.Select(x => x.ToInstance())];
    }

    /// <summary>
    /// Remove the executions started before [cutoff], the instances they ran going along.
    /// </summary>
    public async Task DeleteStartedBeforeAsync(DateTime cutoff)
    {
        await _writeLock.WaitAsync();
        try
        {
            using var connection = _databaseFactory.Create();
            await connection.ExecuteAsync("""
                DELETE FROM TaskInstances WHERE ParentInstanceId IS NULL AND CreatedAt < @cutoff;
                """,
                new { cutoff = cutoff.ToUniversalTime() });
        }
        finally
        {
            _writeLock.Release();
        }
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
                VALUES ({TaskInstanceModel.Values})
                ON CONFLICT (Id) DO NOTHING;
                """,
                TaskInstanceModel.From(instance)) > 0;

            if (!added)
            {
                await connection.ExecuteAsync("""
                    UPDATE TaskInstances SET
                        TaskId = @TaskId,
                        WorkflowVersion = @WorkflowVersion,
                        NodeId = @NodeId,
                        ParentInstanceId = @ParentInstanceId,
                        PreviousIds = @PreviousIds,
                        NodeName = @NodeName,
                        Shared = @Shared,
                        Parameters = @Parameters,
                        Output = @Output,
                        GlobalContext = @GlobalContext,
                        ApplicationId = @ApplicationId,
                        SessionId = @SessionId,
                        NestedVersions = @NestedVersions,
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
