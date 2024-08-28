using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.App.Shared.ApiClients
{
    public class WorkflowClient : BaseCrudClient<WorkflowNode>, IWorkflowClient<WorkflowNode>
    {
        public WorkflowClient(RestClient restClient) : base(restClient, "workflows")
        { }
    }
}
