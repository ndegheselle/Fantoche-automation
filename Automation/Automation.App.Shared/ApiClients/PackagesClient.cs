using Automation.Shared.Base;
using Automation.Shared.Clients;
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
            RestRequest request = new RestRequest($"{_routeBase}/search");
            request.AddQueryParameter("searchValue", searchValue);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);

            return await _client.GetAsync<ListPageWrapper<PackageInfos>>(request) ?? new ListPageWrapper<PackageInfos>();
        }

        public async Task<PackageInfos?> GetByIdAsync(string id)
        {
            RestRequest request = new RestRequest($"{_routeBase}/{id}");
            return await _client.GetAsync<PackageInfos?>(request);
        }

        public async Task<IEnumerable<Version>> GetVersionsAync(string id)
        {
            RestRequest request = new RestRequest($"{_routeBase}/{id}/versions");
            return await _client.GetAsync<IEnumerable<Version>>(request) ?? new List<Version>();
        }

        public async Task<IEnumerable<PackageClass>> GetClassesAsync(string id, Version version)
        {
            RestRequest request = new RestRequest($"{_routeBase}/{id}/versions/{version}/classes");
            return await _client.GetAsync<IEnumerable<PackageClass>>(request) ?? new List<PackageClass>();
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


        public async Task RemoveFromVersionAsync(string id, Version version)
        {
            await _client.DeleteAsync($"{_routeBase}/{id}/versions/{version}");
        }
    }
}
