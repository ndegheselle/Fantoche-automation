using Automation.Dal.Models;
using Automation.Shared.Base;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class ScopesClient : ApiClients.BaseCrudClient<Scope>
    {
        public ScopesClient(RestClient restClient) : base(restClient, "scopes")
        { }

        public async Task<Scope> GetRootAsync()
        {
            return await _client.GetAsync<Scope>($"{_routeBase}/root") ??
                throw new ApiClients.ApiException("Could not get the root scope element.");
        }

        public async Task<IEnumerable<Scope>> GetParentScopes(Guid scopeId)
        {
            return await _client.GetAsync<IEnumerable<Scope>>($"{_routeBase}/{scopeId}/parents") ??
                throw new ApiClients.ApiException("Could not get the parents scopes.");
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
