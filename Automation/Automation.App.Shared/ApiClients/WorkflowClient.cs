using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class WorkflowClient : BaseCrudClient<WorkflowNode>, IWorkflowClient<WorkflowNode>
    {
        public WorkflowClient(RestClient restClient) : base(restClient, "workflows")
        { }
    }
}
