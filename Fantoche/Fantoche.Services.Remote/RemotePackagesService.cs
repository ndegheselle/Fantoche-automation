using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;

namespace Fantoche.Services.Remote;

/// <summary>
/// The packages kept by the supervisor, in the folder it shares with the workers.
/// </summary>
public class RemotePackagesService : IPackagesService
{
    private readonly SupervisorHttp _http;

    public RemotePackagesService(HttpClient client)
    {
        _http = new SupervisorHttp(client);
    }

    public Task<Paginated<PackageInfos>> SearchAsync(string search = "", PaginationOptions options = default)
        => _http.GetAsync<Paginated<PackageInfos>>(
            $"packages?search={SupervisorHttp.Query(search)}&page={options.Page}&pageSize={options.PageSize}");

    public Task<PackageAdded> AddAsync(string filePath)
        => _http.PostFileAsync<PackageAdded>("packages", filePath);

    /// <summary>Every version of [id] when [version] is null.</summary>
    public Task RemoveAsync(string id, Version? version)
    {
        string url = $"packages/{SupervisorHttp.Query(id)}";
        if (version != null)
            url += $"?version={SupervisorHttp.Query(version)}";
        return _http.DeleteAsync(url);
    }

    public async Task<IEnumerable<Version>> GetVersionsAsync(string id)
        => await _http.GetAsync<List<Version>>($"packages/{SupervisorHttp.Query(id)}/versions");

    public Task<List<ClassTarget>> GetClassesAsync(string id, Version version)
        => _http.GetAsync<List<ClassTarget>>($"packages/{SupervisorHttp.Query(id)}/{version}/classes");
}
