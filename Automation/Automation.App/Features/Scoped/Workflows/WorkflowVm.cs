using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Scoped.Workflows;

/// <summary>
/// View model wrapping an <see cref="AutomationWorkflow"/>, displayed by <see cref="WorkflowPage"/>.
/// </summary>
internal class WorkflowVm : ScopedVm
{
    public AutomationWorkflow Workflow => (AutomationWorkflow)Element;

    public WorkflowVm(AutomationWorkflow workflow) : base(workflow)
    {
    }
}
