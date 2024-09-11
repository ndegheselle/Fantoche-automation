using Automation.Shared;
using Automation.Shared.Base;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class PackagesClient : BaseClient, IPackagesClient
    {
        public PackagesClient(RestClient restClient) : base(restClient, "packages") 
        { }

        public async Task<ListPageWrapper<Package>> SearchAsync(string searchValue, int page, int pageSize)
        {
            RestRequest request = new RestRequest($"{_routeBase}");
            request.AddQueryParameter("searchValue", searchValue);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);

            return await _client.GetAsync<ListPageWrapper<Package>>(request) ?? new ListPageWrapper<Package>();
        }
    }
}
