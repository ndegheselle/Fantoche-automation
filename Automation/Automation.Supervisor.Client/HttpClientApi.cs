using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Automation.Supervisor.Client
{
    public class HttpClientApi
    {
        private readonly HttpClient _client;

        public HttpClientApi(HttpClient httpClient)
        {
            _client = httpClient;
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<T?> GetAsync<T>(string uri, Dictionary<string, object>? parameters = null)
        {
            if (parameters != null && parameters.Count > 0)
            {
                var query = HttpUtility.ParseQueryString(string.Empty);
                foreach (var parameter in parameters)
                {
                    query[parameter.Key] = parameter.Value.ToString();
                }
                uri = $"{uri}?{query}";
            }

            HttpResponseMessage response = await _client.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error: {response.StatusCode}");

            string content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }
    }
}
