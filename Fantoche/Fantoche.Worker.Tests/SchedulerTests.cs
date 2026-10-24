using Fantoche.Plugins;
using Fantoche.Services.Local;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The schedules the supervisor starts : what comes due runs on its workers, once whoever else
/// shares its database.
/// </summary>
[TestFixture]
internal sealed class SchedulerTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Schedule Daily = new() { Cron = "0 6 * * *", ParametersJson = """{"Message":"scheduled","Value":1,"Add":10}""" };

    private string _folder = null!;
    private LocalSupervisor _supervisor = null!;
    private LocalWorker _worker = null!;
    private IAsyncDisposable? _registration;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _supervisor = new LocalSupervisor(SupervisorFolder);
        _worker = new LocalWorker(new TestPackageManagement(), Path.Combine(_folder, "worker"));
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_registration != null)
            await _registration.DisposeAsync();
        _worker.Dispose();
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    private string SupervisorFolder => Path.Combine(_folder, "supervisor");

    /// <summary>A stored test task holding the daily schedule.</summary>
    private async Task<AutomationTask> StoreScheduledAsync()
    {
        AutomationTask task = PluginTasks.Make(
            "Fantoche.Plugins.TestTask", "Test", JsonSchema.FromType<Plugins.TestParameters>(), JsonSchema.FromType<TestResult>());
        task.ParentId = Scope.Root.Id;
        task.Schedules = [Daily];
        await _supervisor.Scoped.CreateAsync(task);
        return task;
    }

    /// <summary>Start what the daily schedule has due around its next time with [supervisor].</summary>
    private static Task StartNextDueAsync(LocalSupervisor supervisor)
    {
        DateTime due = Daily.NextOccurrence(Now)!.Value;
        return supervisor.Scheduler.StartDueAsync(due.AddMinutes(-1), due.AddMinutes(1));
    }

    private async Task<TaskInstance> WaitUntilFinishedAsync(Guid runId)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        while (true)
        {
            TaskInstance? run = await _supervisor.History.GetAsync(runId);
            if (run != null && (run.State & EnumTaskState.Finished) != 0)
                return run;
            await Task.Delay(20, timeout.Token);
        }
    }

    [Test]
    public async Task DueSchedule_RunsOnTheWorkersWithItsParameters()
    {
        AutomationTask task = await StoreScheduledAsync();
        _registration = await _supervisor.AddWorkerAsync(_worker, "test-worker", capacity: 1);

        await StartNextDueAsync(_supervisor);

        Guid runId = (await _supervisor.History.GetRunIdsAsync(applicationId: null)).Single();
        TaskInstance run = await WaitUntilFinishedAsync(runId);
        Assert.Multiple(() =>
        {
            Assert.That(run.TaskId, Is.EqualTo(task.Id));
            Assert.That(run.State, Is.EqualTo(EnumTaskState.Completed));
            Assert.That(run.Output?["Value"]?.Value<int>(), Is.EqualTo(11));
        });
    }

    [Test]
    public async Task SupervisorsSharingADatabase_StartADueScheduleOnce()
    {
        await StoreScheduledAsync();
        LocalSupervisor other = new(SupervisorFolder);

        await Task.WhenAll(StartNextDueAsync(_supervisor), StartNextDueAsync(other));

        Assert.That(await _supervisor.History.GetRunIdsAsync(applicationId: null), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ScheduleNotDue_StartsNothing()
    {
        await StoreScheduledAsync();
        DateTime due = Daily.NextOccurrence(Now)!.Value;

        await _supervisor.Scheduler.StartDueAsync(due.AddMinutes(1), due.AddHours(1));

        Assert.That(await _supervisor.History.GetRunIdsAsync(applicationId: null), Is.Empty);
    }
}
