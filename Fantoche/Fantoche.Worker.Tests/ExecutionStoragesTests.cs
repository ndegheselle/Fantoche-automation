using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.Storage;

namespace Fantoche.Worker.Tests;

[TestFixture]
internal sealed class ExecutionStoragesTests
{
    private static readonly Guid TaskId = Guid.NewGuid();

    private string _folder = null!;
    private ExecutionStorages _storages = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _storages = new ExecutionStorages(_folder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    /// <summary>An execution started [minutesAgo], which wrote a file in its storage.</summary>
    private async Task<(TaskInstance Instance, LocalStorage Storage, FileRef File)> RunAsync(int minutesAgo)
    {
        TaskInstance instance = new() { TaskId = TaskId, CreatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo) };
        LocalStorage storage = _storages.Open(TaskId, instance);
        FileRef file = await storage.SaveFileAsync("out.txt", new MemoryStream(Encoding.UTF8.GetBytes("content")));
        return (instance, storage, file);
    }

    private static bool Holds(LocalStorage storage, FileRef file)
    {
        try
        {
            using Stream content = storage.OpenFileAsync(file).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    [Test]
    public async Task Release_KeepsTheStorageOfTheLastExecutionsOnly()
    {
        var oldest = await RunAsync(minutesAgo: 3);
        var middle = await RunAsync(minutesAgo: 2);
        var newest = await RunAsync(minutesAgo: 1);
        _storages.Release(TaskId, oldest.Instance, keep: 2);
        _storages.Release(TaskId, middle.Instance, keep: 2);

        _storages.Release(TaskId, newest.Instance, keep: 2);

        Assert.Multiple(() =>
        {
            Assert.That(Holds(oldest.Storage, oldest.File), Is.False);
            Assert.That(Holds(middle.Storage, middle.File), Is.True);
            Assert.That(Holds(newest.Storage, newest.File), Is.True);
        });
    }

    [Test]
    public async Task Release_SparesTheExecutionsStillRunning()
    {
        var running = await RunAsync(minutesAgo: 2);
        var over = await RunAsync(minutesAgo: 1);

        _storages.Release(TaskId, over.Instance, keep: 0);

        Assert.Multiple(() =>
        {
            Assert.That(Holds(running.Storage, running.File), Is.True);
            Assert.That(Holds(over.Storage, over.File), Is.False);
        });
    }

    [Test]
    public async Task Release_LeavesTheOtherTasksAlone()
    {
        var other = await RunAsync(minutesAgo: 2);
        _storages.Release(TaskId, other.Instance, keep: 1);
        TaskInstance instance = new() { CreatedAt = DateTime.UtcNow };
        _storages.Open(Guid.NewGuid(), instance);

        _storages.Release(Guid.NewGuid(), instance, keep: 0);

        Assert.That(Holds(other.Storage, other.File), Is.True);
    }
}
