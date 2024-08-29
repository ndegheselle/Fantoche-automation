using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TaskClient : BaseCrudClient<TaskNode>, ITaskClient<TaskNode>
    {
        public TaskClient(RestClient restClient) : base(restClient, "tasks")
        { }
    }
}
