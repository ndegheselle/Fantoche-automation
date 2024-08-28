using Automation.Shared;
using RestSharp;
using System.Net.Http;

namespace Automation.App.Shared.ApiClients
{
    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
    }

    public class BaseClient
    {
        protected readonly RestClient _client;
        protected string? _routeBase;

        public BaseClient(RestClient client, string routeBase)
        {
            _client = client;
            _routeBase = routeBase;
        }
    }

    public class BaseCrudClient<T> : BaseClient, ICrudClient<T>
    {
        public BaseCrudClient(RestClient client, string routeBase) : base(client, routeBase)
        {
        }

        public Task<Guid> CreateAsync(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            return _client.PostAsync<Guid>(new RestRequest($"{_routeBase}").AddBody(element));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _client.DeleteAsync($"{_routeBase}/{id}");
        }

        public Task<T?> GetByIdAsync(Guid id)
        {
            return _client.GetAsync<T>($"{_routeBase}/{id}");
        }

        public async Task UpdateAsync(Guid id, T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            await _client.PutAsync(new RestRequest($"{_routeBase}/{id}").AddBody(element));
        }
    }
}
