using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TasksClient : BaseCrudClient<TaskNode>, ITasksClient<TaskNode>
    {
        public TasksClient(RestClient restClient) : base(restClient, "tasks")
        { }
    }
}
