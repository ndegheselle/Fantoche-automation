using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class ScopesClient : BaseCrudClient<Scope>, IScopesClient<Scope>
    {
        public ScopesClient(RestClient restClient) : base(restClient, "scopes")
        { }

        public async Task<Scope> GetRootAsync()
        {
            return await _client.GetAsync<Scope>($"{_routeBase}/root") ??
                throw new ApiException("Could not get the root scope element.");
        }
    }
}
