using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.Storage;

namespace Fantoche.Worker.Tests;

[TestFixture]
internal sealed class ExecutionStoragesTests
{
    private static readonly Guid TaskId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

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

    /// <summary>An execution started [daysAgo], which wrote a file in its storage.</summary>
    private async Task<(LocalStorage Storage, FileRef File)> RunAsync(int daysAgo)
    {
        TaskInstance instance = new() { TaskId = TaskId, CreatedAt = Now.AddDays(-daysAgo) };
        LocalStorage storage = _storages.Open(TaskId, instance);
        FileRef file = await storage.SaveFileAsync("out.txt", new MemoryStream(Encoding.UTF8.GetBytes("content")));
        return (storage, file);
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
    public async Task RemoveStartedBefore_RemovesTheOlderExecutionsOnly()
    {
        var older = await RunAsync(daysAgo: 40);
        var recent = await RunAsync(daysAgo: 2);

        ExecutionStorages.RemoveStartedBefore(_folder, Now.AddMonths(-1));

        Assert.Multiple(() =>
        {
            Assert.That(Holds(older.Storage, older.File), Is.False);
            Assert.That(Holds(recent.Storage, recent.File), Is.True);
        });
    }

    [Test]
    public void RemoveStartedBefore_LeavesTheFoldersItDidntName()
    {
        // A shared volume can hold anything next to the storage.
        string foreign = Path.Combine(_folder, "other", "kept-folder");
        Directory.CreateDirectory(foreign);

        ExecutionStorages.RemoveStartedBefore(_folder, Now);

        Assert.That(Directory.Exists(foreign), Is.True);
    }
}
