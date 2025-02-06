using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Clients;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class GraphsClient : BaseCrudClient<TaskNode>
    {
        public GraphsClient(RestClient restClient) : base(restClient, "graphs")
        {
        }

        public async Task<Graph?> GetByWorkflowId(Guid graphId)
        {
            return await _client.GetAsync<Graph>(
                    new RestRequest($"{_routeBase}/workflows/{graphId}");
        }
    }
}
