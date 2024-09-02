using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using Automation.Shared.Data;
using RestSharp;

namespace Automation.App.Shared.ApiClients
{
    public class WorkflowClient : BaseCrudClient<IWorkflowNode, WorkflowNode>, IWorkflowClient<WorkflowNode>
    {
        public WorkflowClient(RestClient restClient) : base(restClient, "workflows")
        { }
    }
}
