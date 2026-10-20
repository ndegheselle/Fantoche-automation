using Fantoche.Shared.Remote;

namespace Fantoche.Services.Remote;

/// <summary>
/// The workers connected to the supervisor, and what they run.
/// </summary>
public class RemoteWorkersService
{
    private readonly SupervisorHttp _http;

    public RemoteWorkersService(HttpClient client)
    {
        _http = new SupervisorHttp(client);
    }

    public Task<WorkersOverview> GetOverviewAsync() => _http.GetAsync<WorkersOverview>("workers");
}
