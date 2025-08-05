using Automation.Dal.Models;
using Automation.Shared.Base;
using RestSharp;
using System.Threading.Tasks;

namespace Automation.App.Shared.ApiClients
{
    public class TasksClient : BaseCrudClient<BaseAutomationTask>
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

        public async Task<IEnumerable<string>> GetTagsAsync()
        {
            var request = new RestRequest($"{_routeBase}/tags");
            return await _client.GetAsync<IEnumerable<string>>(request) ??
                throw new Exception("Could not get the tags from the server.");
        }

        public async Task<IEnumerable<BaseAutomationTask>> GetByTagAsync(string tag)
        {
            var request = new RestRequest($"{_routeBase}/tags/{tag}");
            return await _client.GetAsync<IEnumerable<BaseAutomationTask>>(request) ??
                throw new Exception("Could not get the tasks from the server.");
        }
    }
}
