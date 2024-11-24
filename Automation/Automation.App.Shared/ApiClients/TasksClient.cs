using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Base;
using Automation.Shared.Clients;
using RestSharp;
using System.Threading.Tasks;

namespace Automation.App.Shared.ApiClients
{
    public class TasksClient : BaseCrudClient<TaskNode>
    {
        public TasksClient(RestClient restClient) : base(restClient, "tasks")
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

        public async Task<TaskInstance> ExecuteAsync(Guid taskId, object? settings)
        {
            return await _client.PostAsync<TaskInstance>(
                    new RestRequest($"{_routeBase}/{taskId}/execute").AddBody(settings)) ??
                throw new Exception("Could not get the task instance from the server.");
        }
    }
}
