using Automation.Models.Work;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class TaskInstancesClient : BaseCrudClient<NodeInstance>
    {
        public TaskInstancesClient(RestClient restClient) : base(restClient, "instances")
        { }
    }
}
