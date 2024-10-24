using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Clients;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class WorkflowsClient : BaseCrudClient<WorkflowNode>
    {
        public WorkflowsClient(RestClient restClient) : base(restClient, "workflows")
        { }
    }
}
