using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Storage;

/// <summary>
/// The storage of an execution following it to the worker running it, and back : a worker only
/// reads its own storage, so the tables and files an execution is handed are sent along with it,
/// and the ones it hands over are sent back to its caller.
/// </summary>
public static class StorageTransfer
{
    /// <summary>
    /// Put in transit for [executionId] every item of [storage] [token] points at. The ones the
    /// storage doesn't hold are left out : the task reading them fails, naming them.
    /// </summary>
    public static async Task SendAsync(LocalStorage storage, JToken? token, IStorageTransit transit, Guid executionId)
    {
        IEnumerable<string> items = LocalStorage.IdsIn(token).SelectMany(storage.ItemsOf);
        foreach (string item in items)
        {
            await using Stream content = storage.OpenItem(item);
            await transit.PutAsync(executionId, item, content);
        }
    }

    /// <summary>
    /// Take every item in transit for [executionId] into [storage], then clear the transit.
    /// </summary>
    public static async Task ReceiveAsync(IStorageTransit transit, Guid executionId, LocalStorage storage)
    {
        foreach (string item in await transit.GetItemsAsync(executionId))
        {
            // An item never changes : one already there is the same.
            if (storage.HasItem(item))
                continue;

            await using Stream content = await transit.OpenAsync(executionId, item);
            await storage.ImportItemAsync(item, content);
        }

        await transit.ClearAsync(executionId);
    }
}
