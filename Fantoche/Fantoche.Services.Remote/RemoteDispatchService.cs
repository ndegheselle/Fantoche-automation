using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Fantoche.Services.Remote;

/// <summary>
/// What the supervisor dispatches. Its changes are pushed by the supervisor once
/// <see cref="StartAsync"/> connected to it.
/// </summary>
public sealed class RemoteDispatchService : IDispatchService, IAsyncDisposable
{
    private readonly SupervisorHttp _http;
    private readonly HubConnection _events;

    public event Action<DispatchStatus>? StatusChanged;

    public RemoteDispatchService(HttpClient client)
    {
        _http = new SupervisorHttp(client);
        Uri supervisor = client.BaseAddress ?? throw new ArgumentException("The client has no supervisor address.", nameof(client));

        _events = new HubConnectionBuilder()
            .WithUrl(new Uri(supervisor, SupervisorRoutes.Dispatch))
            .WithAutomaticReconnect(new RetryForever())
            .AddJsonProtocol(options => FantocheJson.Configure(options.PayloadSerializerOptions))
            .Build();
        _events.On<DispatchStatus>(nameof(IDispatchClient.StatusChanged), x => StatusChanged?.Invoke(x));
        // The changes pushed while disconnected are lost : the status is read again.
        _events.Reconnected += async _ => StatusChanged?.Invoke(await GetStatusAsync());
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

    public Task<DispatchStatus> GetStatusAsync() => _http.GetAsync<DispatchStatus>("dispatch/status");

    public Task ResumeAsync(Guid runId) => _http.PostAsync($"dispatch/runs/{runId}/resume");
}
