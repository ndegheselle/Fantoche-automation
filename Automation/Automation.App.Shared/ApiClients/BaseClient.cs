using Automation.Shared;
using RestSharp;

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

    public class BaseCrudClient<TIn, TOut> : BaseClient, ICrudClient<TIn, TOut>
    {
        public BaseCrudClient(RestClient client, string routeBase) : base(client, routeBase)
        {
        }

        public async Task<Guid> CreateAsync(TIn element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            return await _client.PostAsync<Guid>(new RestRequest($"{_routeBase}").AddBody(element));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _client.DeleteAsync($"{_routeBase}/{id}");
        }

        public Task<TOut?> GetByIdAsync(Guid id)
        {
            return _client.GetAsync<TOut>($"{_routeBase}/{id}");
        }

        public async Task UpdateAsync(Guid id, TIn element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            await _client.PutAsync(new RestRequest($"{_routeBase}/{id}").AddBody(element));
        }
    }
}
