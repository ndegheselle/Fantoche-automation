using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Base;
using Automation.Shared.Clients;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TaskInstancesClient : BaseCrudClient<TaskInstance>
    {
        public TaskInstancesClient(RestClient restClient) : base(restClient, "tasks")
        { }
    }
}
