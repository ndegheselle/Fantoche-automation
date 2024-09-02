using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using Automation.Shared.Data;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TaskClient : BaseCrudClient<ITaskNode, TaskNode>, ITaskClient<TaskNode>
    {
        public TaskClient(RestClient restClient) : base(restClient, "tasks")
        { }
    }
}
