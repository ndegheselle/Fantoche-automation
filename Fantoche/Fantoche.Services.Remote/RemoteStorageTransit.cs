using Fantoche.Worker.Storage;

namespace Fantoche.Services.Remote;

/// <summary>
/// The storage in transit kept by the supervisor.
/// </summary>
public sealed class RemoteStorageTransit : IStorageTransit
{
    private readonly SupervisorHttp _http;

    public RemoteStorageTransit(HttpClient client)
    {
        _http = new SupervisorHttp(client);
    }

    public Task PutAsync(Guid executionId, string item, Stream content)
        => _http.PutStreamAsync(UrlOf(executionId, item), content);

    public async Task<IReadOnlyList<string>> GetItemsAsync(Guid executionId)
        => await _http.GetAsync<List<string>>($"transit/{executionId}");

    public Task<Stream> OpenAsync(Guid executionId, string item)
        => _http.OpenStreamAsync(UrlOf(executionId, item));

    public Task ClearAsync(Guid executionId)
        => _http.DeleteAsync($"transit/{executionId}");

    /// <summary>Each part of the path of [item] escaped on its own, so it keeps its folders.</summary>
    private static string UrlOf(Guid executionId, string item)
        => $"transit/{executionId}/{string.Join('/', item.Split('/').Select(SupervisorHttp.Query))}";
}
