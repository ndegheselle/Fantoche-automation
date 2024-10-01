using Automation.Shared;
using Automation.Shared.Base;
using Newtonsoft.Json;
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

    public class BaseCrudClient<T> : BaseClient, ICrudClient<T>
    {
        public BaseCrudClient(RestClient client, string routeBase) : base(client, routeBase)
        {
        }

        public virtual async Task<Guid> CreateAsync(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var result = await _client.ExecutePostAsync<Guid>(new RestRequest($"{_routeBase}").AddBody(element));
            if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var validationResult = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(result.Content);
                throw new ValidationException(validationResult);
            }

            return result.Data;
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _client.DeleteAsync($"{_routeBase}/{id}");
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _client.GetAsync<T>($"{_routeBase}/{id}");
        }

        public virtual async Task UpdateAsync(Guid id, T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            var result = await _client.ExecutePutAsync(new RestRequest($"{_routeBase}/{id}").AddBody(element));
        }
    }
}
