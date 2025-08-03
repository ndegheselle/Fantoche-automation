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

    public class BaseCrudClient<T> : BaseClient
    {
        public BaseCrudClient(RestClient client, string routeBase) : base(client, routeBase)
        {
        }

        #region GET
        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _client.GetAsync<T>($"{_routeBase}/{id}");
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _client.GetAsync<IEnumerable<T>>($"{_routeBase}") ?? [];
        }
        #endregion

        #region POST
        public virtual async Task<Guid> CreateAsync(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var result = await _client.ExecutePostAsync<Guid>(new RestRequest($"{_routeBase}").AddBody(element));
            if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                if (string.IsNullOrEmpty(result.Content))
                    throw new ValidationException(null);
                var validationResult = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(result.Content);
                throw new ValidationException(validationResult);
            }
            if (result.StatusCode != System.Net.HttpStatusCode.OK && result.StatusCode != System.Net.HttpStatusCode.Created)
                throw new ApiException($"An unexpected error happened.");

            return result.Data;
        }
        #endregion

        #region PUT
        public virtual async Task UpdateAsync(Guid id, T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            var result = await _client.ExecutePutAsync(new RestRequest($"{_routeBase}/{id}").AddBody(element));
        }
        #endregion

        #region DELETE
        public virtual async Task DeleteAsync(Guid id)
        {
            await _client.DeleteAsync($"{_routeBase}/{id}");
        }
        #endregion
    }
}
