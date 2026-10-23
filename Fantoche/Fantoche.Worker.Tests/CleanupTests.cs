using System.Text;
using Fantoche.Services.Local;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Worker.Storage;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The executions the supervisor keeps : a month of them, their history and their storage.
/// </summary>
[TestFixture]
internal sealed class CleanupTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    private string _folder = null!;
    private string _storageFolder = null!;
    private LocalSupervisor _supervisor = null!;
    /// <summary>The task the runs are executions of : an instance points at a stored task.</summary>
    private Guid _taskId;

    [SetUp]
    public async Task SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _storageFolder = Path.Combine(_folder, "storage");
        _supervisor = new LocalSupervisor(Path.Combine(_folder, "supervisor"), _storageFolder);

        AutomationTask task = PluginTasks.Make("Fantoche.Plugins.TestTask", "Test", new JsonSchema(), new JsonSchema());
        task.ParentId = Scope.Root.Id;
        await _supervisor.Scoped.CreateAsync(task);
        _taskId = task.Id;
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    /// <summary>A run started [daysAgo] with a node, stored and having written in its storage.</summary>
    private async Task<(TaskInstance Run, TaskInstance Node, string Storage)> RunAsync(int daysAgo)
    {
        TaskInstance run = new() { TaskId = _taskId, NodeName = "Run", CreatedAt = Now.AddDays(-daysAgo) };
        TaskInstance node = new() { TaskId = _taskId, NodeName = "Node", ParentInstanceId = run.Id, CreatedAt = run.CreatedAt };
        await _supervisor.History.SaveAsync(run);
        await _supervisor.History.SaveAsync(node);

        var storage = new ExecutionStorages(_storageFolder).Open(_taskId, run);
        await storage.SaveFileAsync("out.txt", new MemoryStream(Encoding.UTF8.GetBytes("content")));
        string folder = Directory.GetDirectories(Path.Combine(_storageFolder, _taskId.ToString("N")))
            .Single(x => x.EndsWith(run.Id.ToString("N")));
        return (run, node, folder);
    }

    [Test]
    public async Task CleanUp_RemovesTheRunsOlderThanAMonth()
    {
        var older = await RunAsync(daysAgo: 40);

        await _supervisor.CleanUpAsync(Now);

        Assert.Multiple(async () =>
        {
            Assert.That(await _supervisor.History.GetAsync(older.Run.Id), Is.Null);
            Assert.That(await _supervisor.History.GetAsync(older.Node.Id), Is.Null);
            Assert.That(Directory.Exists(older.Storage), Is.False);
        });
    }

    [Test]
    public async Task CleanUp_KeepsTheRunsOfTheLastMonth()
    {
        var recent = await RunAsync(daysAgo: 20);

        await _supervisor.CleanUpAsync(Now);

        Assert.Multiple(async () =>
        {
            Assert.That(await _supervisor.History.GetAsync(recent.Run.Id), Is.Not.Null);
            Assert.That(await _supervisor.History.GetAsync(recent.Node.Id), Is.Not.Null);
            Assert.That(Directory.Exists(recent.Storage), Is.True);
        });
    }
}
