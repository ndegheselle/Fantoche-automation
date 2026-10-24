namespace Fantoche.Services.Local.Models;

/// <summary>
/// One row of the scheduled starts table : a time a schedule came due, claimed by the supervisor
/// starting it. The supervisors sharing a database (copies of the application) all see it due, the
/// claim is what has a single one of them start it (see <see cref="Scheduler"/>).
/// </summary>
internal static class ScheduledStartModel
{
    /// <summary>
    /// A schedule has no id of its own : it is told apart from the other ones of its element by what
    /// it holds, two schedules holding the same being the same.
    /// </summary>
    public static readonly string Schema = """
        CREATE TABLE IF NOT EXISTS ScheduledStarts (
            TaskId TEXT NOT NULL REFERENCES Scoped (Id) ON DELETE CASCADE,
            Cron TEXT NOT NULL,
            Parameters TEXT NOT NULL,
            DueAt TEXT NOT NULL,
            PRIMARY KEY (TaskId, Cron, Parameters, DueAt)
        );
        """;
}
