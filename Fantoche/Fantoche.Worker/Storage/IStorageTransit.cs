namespace Fantoche.Worker.Storage;

/// <summary>
/// Where the storage of an execution sent to a worker crosses over, held by the supervisor : what
/// its parameters point at until the worker took it, then what its output points at until the
/// caller took it back (see <see cref="StorageTransfer"/>). An execution is known by the id of its
/// instance, an item by its path in a <see cref="LocalStorage"/>.
/// </summary>
public interface IStorageTransit
{
    Task PutAsync(Guid executionId, string item, Stream content);

    /// <summary>The items in transit for [executionId], empty when there are none.</summary>
    Task<IReadOnlyList<string>> GetItemsAsync(Guid executionId);

    Task<Stream> OpenAsync(Guid executionId, string item);

    /// <summary>Forget everything in transit for [executionId].</summary>
    Task ClearAsync(Guid executionId);
}
