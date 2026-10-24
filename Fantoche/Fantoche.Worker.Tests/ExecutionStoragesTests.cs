using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.Storage;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The storage of the runs of an application, out of reach of the supervisor : what the history
/// doesn't hold anymore goes.
/// </summary>
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

    /// <summary>A run which wrote a file in its storage.</summary>
    private async Task<(TaskInstance Run, LocalStorage Storage, FileRef File)> RunAsync()
    {
        TaskInstance run = new() { TaskId = TaskId };
        LocalStorage storage = _storages.Open(TaskId, run);
        FileRef file = await storage.SaveFileAsync("out.txt", new MemoryStream(Encoding.UTF8.GetBytes("content")));
        return (run, storage, file);
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
    public async Task RemoveAllBut_RemovesTheRunsTheHistoryDoesntHold()
    {
        var removed = await RunAsync();
        var kept = await RunAsync();

        _storages.RemoveAllBut(new HashSet<Guid> { kept.Run.Id });

        Assert.Multiple(() =>
        {
            Assert.That(Holds(removed.Storage, removed.File), Is.False);
            Assert.That(Holds(kept.Storage, kept.File), Is.True);
        });
    }

    [Test]
    public void RemoveAllBut_LeavesTheFoldersItDidntName()
    {
        // A shared volume can hold anything next to the storage.
        string foreign = Path.Combine(_folder, "other", "kept-folder");
        Directory.CreateDirectory(foreign);

        _storages.RemoveAllBut(new HashSet<Guid>());

        Assert.That(Directory.Exists(foreign), Is.True);
    }
}
