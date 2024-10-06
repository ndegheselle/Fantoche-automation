using Automation.Shared;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Newtonsoft.Json;
using RestSharp;
using System.IO;

namespace Automation.App.Shared.ApiClients
{
    public class PackagesClient : BaseClient, IPackagesClient
    {
        public PackagesClient(RestClient restClient) : base(restClient, "packages") 
        { }

        public async Task<ListPageWrapper<PackageInfos>> SearchAsync(string searchValue, int page, int pageSize)
        {
            RestRequest request = new RestRequest($"{_routeBase}");
            request.AddQueryParameter("searchValue", searchValue);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);

            return await _client.GetAsync<ListPageWrapper<PackageInfos>>(request) ?? new ListPageWrapper<PackageInfos>();
        }

        public async Task<PackageInfos?> GetById(string id)
        {
            RestRequest request = new RestRequest($"{_routeBase}/{id}");
            return await _client.GetAsync<PackageInfos?>(request);
        }

        public async Task<PackageInfos> CreateAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var result = await _client.ExecutePostAsync<PackageInfos>(new RestRequest($"{_routeBase}")
                .AddFile(Path.GetFileName(filePath), filePath));

            if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var validationResult = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(result.Content);
                throw new ValidationException(validationResult);
            }

            return result.Data;
        }

        public async Task<PackageInfos> CreatePackageVersionAsync(string id, string filePath)
        {
            if (!File.Exists(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var request = new RestRequest($"{_routeBase}/{id}/versions", Method.Post);
            request.AlwaysMultipartFormData = true;
            request.AddFile("file", filePath);

            var result = await _client.ExecuteAsync<PackageInfos>(request);

            if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var validationResult = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(result.Content);
                throw new ValidationException(validationResult);
            }
            return result.Data;
        }


        public async Task<PackageInfos?> RemoveFromVersionAsync(string id, Version version)
        {
            return await _client.DeleteAsync<PackageInfos?>($"{_routeBase}/{id}/versions/{version}");
        }
    }
}
