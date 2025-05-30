using Automation.App.Shared.ViewModels.Work;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class GraphsClient : ApiClients.BaseCrudClient<Graph>
    {
        public GraphsClient(RestClient restClient) : base(restClient, "graphs")
        {
        }

        public async Task<Graph?> GetByWorkflowIdAsync(Guid graphId)
        {
            return await _client.GetAsync<Graph>(
                    new RestRequest($"{_routeBase}/workflows/{graphId}"));
        }
    }
}
