using Dapper;
using Fantoche.Services.Local.Database;
using Fantoche.Services.Local.Models;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Local;

/// <summary>
/// Start the tasks and workflows on their schedules (see <see cref="Schedule"/>), on the workers of
/// the <see cref="Dispatcher"/>.
/// <para>
/// Checked every minute, the finest a cron goes. What came due while no supervisor ran is skipped :
/// a supervisor down for a night doesn't start the runs of the night all at once in the morning.
/// </para>
/// <para>
/// The supervisors sharing a database (copies of the application in standalone mode) all check the
/// schedules : each time a schedule comes due is claimed in the database first, so a single one of
/// them starts it.
/// </para>
/// </summary>
public sealed class Scheduler
{
    private readonly DatabaseFactory _database;
    private readonly Dispatcher _dispatcher;

    public Scheduler(DatabaseFactory database, Dispatcher dispatcher)
    {
        _database = database;
        _dispatcher = dispatcher;
    }

    /// <summary>Start what came due from [from], included, to [to], excluded, and nobody claimed yet.</summary>
    public async Task StartDueAsync(DateTime from, DateTime to)
    {
        IReadOnlyList<BaseAutomationTask> scheduled = await GetScheduledAsync();
        var due = scheduled.SelectMany(task => task.Schedules.SelectMany(schedule => schedule
            .OccurrencesBetween(from, to)
            .Select(dueAt => (Task: task, Schedule: schedule, DueAt: dueAt))));

        foreach (var (task, schedule, dueAt) in due)
        {
            if (!await ClaimAsync(task.Id, schedule, dueAt))
                continue;

            JToken? parameters = schedule.ParametersJson == null ? null : JToken.Parse(schedule.ParametersJson);
            await _dispatcher.StartAsync(task.Id, task.Metadata.Name, parameters);
        }
    }

    /// <summary>Start what comes due, every minute until [cancellation].</summary>
    public async Task RunAsync(CancellationToken cancellation)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));
        DateTime checkedUntil = DateTime.UtcNow;
        try
        {
            while (await timer.WaitForNextTickAsync(cancellation))
            {
                DateTime now = DateTime.UtcNow;
                try
                {
                    await StartDueAsync(checkedUntil, now);
                    checkedUntil = now;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Checked again from the same time on the next tick : what was already claimed
                    // isn't started twice.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping.
        }
    }

    /// <summary>Forget the claims of what came due before [cutoff], long checked.</summary>
    public async Task ForgetBeforeAsync(DateTime cutoff)
    {
        using var connection = _database.Create();
        await connection.ExecuteAsync("""
            DELETE FROM ScheduledStarts WHERE DueAt < @cutoff;
            """,
            new { cutoff = cutoff.ToUniversalTime() });
    }

    /// <summary>
    /// The tasks and workflows having schedules, as much as the scheduler reads of them : the graph
    /// of a workflow isn't loaded, the dispatcher reads the workflow again as it starts it.
    /// </summary>
    private async Task<IReadOnlyList<BaseAutomationTask>> GetScheduledAsync()
    {
        using var connection = _database.Create();
        var rows = await connection.QueryAsync<ScopedModel>($"""
            SELECT {ScopedModel.Columns} FROM Scoped WHERE Schedules IS NOT NULL AND Schedules <> '[]';
            """);
        return [.. rows.Select(x => x.ToElement()).OfType<BaseAutomationTask>()];
    }

    /// <summary>Whether this supervisor is the first to claim [schedule] of [taskId] coming due at [dueAt].</summary>
    private async Task<bool> ClaimAsync(Guid taskId, Schedule schedule, DateTime dueAt)
    {
        using var connection = _database.Create();
        // In one statement : another supervisor claiming it at the same time is refused by the key.
        int claimed = await connection.ExecuteAsync("""
            INSERT OR IGNORE INTO ScheduledStarts (TaskId, Cron, Parameters, DueAt)
            VALUES (@taskId, @cron, @parameters, @dueAt);
            """,
            new { taskId, cron = schedule.Cron, parameters = schedule.ParametersJson ?? "", dueAt = dueAt.ToUniversalTime() });
        return claimed == 1;
    }
}
