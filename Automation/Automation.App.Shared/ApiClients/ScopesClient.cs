using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Base;
using Automation.Shared.Clients;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class ScopesClient : BaseCrudClient<Scope>
    {
        public ScopesClient(RestClient restClient) : base(restClient, "scopes")
        { }

        public async Task<Scope> GetRootAsync()
        {
            return await _client.GetAsync<Scope>($"{_routeBase}/root") ??
                throw new ApiException("Could not get the root scope element.");
        }

        public async Task<ListPageWrapper<TaskInstance>> GetInstancesAsync(Guid scopeId, int page, int pageSize)
        {
            return await _client.GetAsync<ListPageWrapper<TaskInstance>>(
                    new RestRequest($"{_routeBase}/{scopeId}/instances")
                .AddParameter("page", page)
                .AddParameter("pageSize", pageSize)) ??
                new ListPageWrapper<TaskInstance>();
        }
    }
}
