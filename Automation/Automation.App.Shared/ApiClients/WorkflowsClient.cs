using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class WorkflowsClient : BaseCrudClient<WorkflowNode>, IWorkflowsClient<WorkflowNode>
    {
        public WorkflowsClient(RestClient restClient) : base(restClient, "workflows")
        { }
    }
}
