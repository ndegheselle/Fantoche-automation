using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using Automation.Shared.Data;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class ScopeClient : BaseCrudClient<IScope, Scope>, IScopeClient<Scope>
    {
        public ScopeClient(RestClient restClient) : base(restClient, "scopes")
        { }

        public async Task<Scope> GetRootAsync()
        {
            return await _client.GetAsync<Scope>($"{_routeBase}/root") ??
                throw new ApiException("Could not get the root scope element.");
        }
    }
}
