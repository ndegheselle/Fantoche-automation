using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;

namespace Automation.App.Shared.ApiClients
{
    public partial class ScopeClient : IScopeClient<Scope>
    { }

    public partial class TaskClient : ITaskClient<TaskNode>
    { }

    public partial class WorkflowClient : IWorkflowClient<WorkflowNode>
    { }


    public partial class HistoryClient : IHistoryClient<TaskHistory>
    { }
}
