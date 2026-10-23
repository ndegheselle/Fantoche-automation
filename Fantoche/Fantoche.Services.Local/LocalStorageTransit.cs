using Fantoche.Worker.Storage;

namespace Fantoche.Services.Local;

/// <summary>
/// The storage in transit, in a folder : <c>{folder}/{executionId}/{item}</c>, an item keeping the
/// path it has in a <see cref="LocalStorage"/>.
/// </summary>
public sealed class LocalStorageTransit : IStorageTransit
{
    private readonly string _folder;

    public LocalStorageTransit(string folder)
    {
        _folder = folder;
    }

    /// <summary>
    /// Forget everything in transit : the executions only live in the memory of the supervisor,
    /// none of them survived it stopping.
    /// </summary>
    public void Reset()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    public async Task PutAsync(Guid executionId, string item, Stream content)
    {
        string path = PathOf(executionId, item);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using FileStream stream = File.Create(path);
        await content.CopyToAsync(stream);
    }

    public Task<IReadOnlyList<string>> GetItemsAsync(Guid executionId)
    {
        string folder = FolderOf(executionId);
        IReadOnlyList<string> items = Directory.Exists(folder)
            ? [.. Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Select(x => Path.GetRelativePath(folder, x).Replace('\\', '/'))]
            : [];
        return Task.FromResult(items);
    }

    public Task<Stream> OpenAsync(Guid executionId, string item)
        => Task.FromResult<Stream>(File.OpenRead(PathOf(executionId, item)));

    public Task ClearAsync(Guid executionId)
    {
        string folder = FolderOf(executionId);
        if (Directory.Exists(folder))
            Directory.Delete(folder, true);
        return Task.CompletedTask;
    }

    private string FolderOf(Guid executionId) => Path.Combine(_folder, executionId.ToString("N"));

    /// <summary>Where [item] is kept, only once checked to be an item of a storage.</summary>
    private string PathOf(Guid executionId, string item)
    {
        if (!LocalStorage.IsItem(item))
            throw new ArgumentException($"[{item}] is not an item of a storage.", nameof(item));
        return Path.Combine(FolderOf(executionId), item);
    }
}
