using Automation.App.Shared.ViewModels;
using Automation.Shared;

namespace Automation.App.Shared.ApiClients
{
    public partial class ScopeClient : IScopeRepository<Scope>
    { }

    public partial class TaskClient : ITaskRepository<TaskNode>
    { }

    public partial class WorkflowClient : IWorkflowRepository<WorkflowNode>
    { }
}
