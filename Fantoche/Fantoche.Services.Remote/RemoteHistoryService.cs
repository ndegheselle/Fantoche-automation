using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Remote;

/// <summary>
/// The history stored by the supervisor. Its changes are pushed by the supervisor whoever made them
/// (this application, another one or a worker), once <see cref="StartAsync"/> connected to it.
/// </summary>
public sealed class RemoteHistoryService : IHistoryService, IAsyncDisposable
{
    private readonly SupervisorHttp _http;
    private readonly HubConnection _events;

    public event Action<TaskInstance>? InstanceAdded;
    public event Action<TaskInstance>? InstanceUpdated;

    public RemoteHistoryService(HttpClient client)
    {
        _http = new SupervisorHttp(client);
        Uri supervisor = client.BaseAddress ?? throw new ArgumentException("The client has no supervisor address.", nameof(client));

        _events = new HubConnectionBuilder()
            .WithUrl(new Uri(supervisor, SupervisorRoutes.History))
            .WithAutomaticReconnect(new RetryForever())
            .AddJsonProtocol(options => FantocheJson.Configure(options.PayloadSerializerOptions))
            .Build();
        _events.On<TaskInstance>(nameof(IHistoryClient.InstanceAdded), x => InstanceAdded?.Invoke(x));
        _events.On<TaskInstance>(nameof(IHistoryClient.InstanceUpdated), x => InstanceUpdated?.Invoke(x));
    }

    public ValueTask DisposeAsync() => _events.DisposeAsync();

    /// <summary>
    /// Listen to the changes, waiting for the supervisor to be up.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellation = default)
    {
        while (true)
        {
            try
            {
                await _events.StartAsync(cancellation);
                return;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellation);
            }
        }
    }

    public Task<Paginated<TaskInstance>> GetByScopedAsync(Guid elementId, PaginationOptions options = default)
        => _http.GetAsync<Paginated<TaskInstance>>($"history/elements/{elementId}?page={options.Page}&pageSize={options.PageSize}");

    public async Task<IReadOnlyList<TaskInstance>> GetChildrenAsync(Guid instanceId)
        => await _http.GetAsync<List<TaskInstance>>($"history/instances/{instanceId}/children");

    public async Task<IReadOnlyList<TaskInstance>> GetDescendantsAsync(Guid instanceId)
        => await _http.GetAsync<List<TaskInstance>>($"history/instances/{instanceId}/descendants");

    public async Task<IReadOnlyList<Guid>> GetRunIdsAsync(string? applicationId)
    {
        string query = applicationId == null ? "" : $"?applicationId={SupervisorHttp.Query(applicationId)}";
        return await _http.GetAsync<List<Guid>>($"history/runs/ids{query}");
    }

    public async Task<IReadOnlyList<TaskInstance>> GetUnfinishedRunsAsync(string? applicationId)
    {
        string query = applicationId == null ? "" : $"?applicationId={SupervisorHttp.Query(applicationId)}";
        return await _http.GetAsync<List<TaskInstance>>($"history/runs/unfinished{query}");
    }

    public Task SuspendRunAsync(Guid runId, string reason)
        => _http.PostAsync($"history/runs/{runId}/suspend?reason={SupervisorHttp.Query(reason)}");

    public Task<bool> ClaimRunAsync(Guid runId, string sessionId)
        => _http.PostAsync<bool>($"history/runs/{runId}/claim?sessionId={SupervisorHttp.Query(sessionId)}");

    public Task<TaskInstance?> GetAsync(Guid instanceId)
        => _http.FindAsync<TaskInstance>($"history/instances/{instanceId}");

    /// <summary>
    /// Sent as a plain instance : what a workflow instance holds besides is only known by its run.
    /// </summary>
    public Task SaveAsync(TaskInstance instance)
        => _http.SendAsync<TaskInstance>(HttpMethod.Put, "history/instances", instance);
}
