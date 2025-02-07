using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Base;
using Automation.Shared.Clients;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TasksClient : BaseCrudClient<AutomationTask>
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
            var request = new RestRequest($"{_routeBase}/{taskId}/execute");

            if (settings != null)
                request.AddBody(settings);

            return await _client.PostAsync<TaskInstance>(request) ??
                throw new Exception("Could not get the task instance from the server.");
        }
    }
}
