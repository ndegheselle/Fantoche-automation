using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Base;
using Automation.Shared.Clients;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class GraphsClient : BaseCrudClient<TaskNode>
    {
        public GraphsClient(RestClient restClient) : base(restClient, "graphs")
        {
        }

        public async Task<ListPageWrapper<TaskInstance>> GetInstancesAsync(Guid taskId, int page, int pageSize)
        {
            return await _client.GetAsync<ListPageWrapper<TaskInstance>>(
                    new RestRequest($"{_routeBase}/{taskId}/instances")
                .AddParameter("page", page)
                .AddParameter("pageSize", pageSize)) ??
                new ListPageWrapper<TaskInstance>();
        }
    }
}
