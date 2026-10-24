using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Remote;

/// <summary>
/// The scopes, tasks and workflows stored by the supervisor.
/// </summary>
public class RemoteScopedService : IScopedService
{
    private readonly SupervisorHttp _http;

    public RemoteScopedService(HttpClient client)
    {
        _http = new SupervisorHttp(client);
    }

    public Task<List<ScopedElement>> GetChildrensAsync(Guid scopeId)
        => _http.GetAsync<List<ScopedElement>>($"elements/{scopeId}/children");

    public Task<List<ScopedElement>> GetTreeAsync()
        => _http.GetAsync<List<ScopedElement>>("elements/tree");

    public Task<ScopedElement?> GetAsync(Guid elementId)
        => _http.FindAsync<ScopedElement>($"elements/{elementId}");

    public async Task<AutomationWorkflow?> GetVersionAsync(Guid workflowId, int version)
        => await _http.FindAsync<ScopedElement>($"elements/{workflowId}/versions/{version}") as AutomationWorkflow;

    public Task<List<ScopedElement>> GetAsync(IReadOnlyCollection<Guid> elementIds)
        => _http.SendAsync<IReadOnlyCollection<Guid>, List<ScopedElement>>(HttpMethod.Post, "elements/query", elementIds);

    public Task<List<Guid>> GetGraphTaskIdsAsync(Guid workflowId)
        => _http.GetAsync<List<Guid>>($"elements/{workflowId}/graph-task-ids");

    public Task<Paginated<BaseAutomationTask>> SearchAsync(string search = "", PaginationOptions options = default)
        => _http.GetAsync<Paginated<BaseAutomationTask>>(
            $"elements/search?search={SupervisorHttp.Query(search)}&page={options.Page}&pageSize={options.PageSize}");

    public Task<List<ScopedElement>> SearchTreeAsync(string search = "")
        => _http.GetAsync<List<ScopedElement>>($"elements/search-tree?search={SupervisorHttp.Query(search)}");

    /// <summary>
    /// [element] is given the id it was stored under, the way the local service does.
    /// </summary>
    public async Task<ScopedElement> CreateAsync(ScopedElement element)
    {
        ScopedElement created = await _http.SendAsync<ScopedElement, ScopedElement>(HttpMethod.Post, "elements", element);
        element.Id = created.Id;
        return element;
    }

    /// <summary>[element] is given the version the supervisor stored its graph as, the way the local service does.</summary>
    public async Task<ScopedElement> EditAsync(ScopedElement element)
    {
        ScopedElement edited = await _http.SendAsync<ScopedElement, ScopedElement>(HttpMethod.Put, $"elements/{element.Id}", element);
        if (element is AutomationWorkflow workflow && edited is AutomationWorkflow stored)
            workflow.Version = stored.Version;
        return element;
    }

    public async Task<ScopedElement> RemoveAsync(ScopedElement element)
    {
        await _http.DeleteAsync<ScopedElement>($"elements/{element.Id}");
        return element;
    }

    public Task<List<TaskUsage>> GetUsagesAsync(Guid taskId)
        => _http.GetAsync<List<TaskUsage>>($"elements/{taskId}/usages");

    public Task<JObject> GetContextAsync(Guid elementId)
        => _http.GetAsync<JObject>($"elements/{elementId}/context");

    public Task<bool> IsNameUniqueAsync(Guid parentId, string name, Guid? excludeId = null)
    {
        string url = $"elements/{parentId}/name-unique?name={SupervisorHttp.Query(name)}";
        if (excludeId != null)
            url += $"&excludeId={excludeId}";
        return _http.GetAsync<bool>(url);
    }
}
