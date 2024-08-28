using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.App.Shared.ApiClients
{
    public class ScopeClient : BaseCrudClient<Scope>, IScopeClient<Scope>
    {
        public ScopeClient(RestClient restClient) : base(restClient, "scopes")
        { }

        public async Task<Scope> GetRootAsync()
        {
            return await this.GetAsync<Scope>($"{_routeBase}/root") ??
                throw new ApiException("Could not get the root scope element.");
        }
    }
}
