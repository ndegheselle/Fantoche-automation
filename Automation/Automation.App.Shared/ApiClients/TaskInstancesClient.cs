using Automation.App.Shared.ViewModels.Work;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TaskInstancesClient : BaseCrudClient<AutomationTaskInstance>
    {
        public TaskInstancesClient(RestClient restClient) : base(restClient, "instances")
        { }
    }
}
