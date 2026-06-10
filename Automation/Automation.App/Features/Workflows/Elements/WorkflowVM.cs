using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Workflows.Elements;

/// <summary>
/// View model wrapping an <see cref="AutomationWorkflow"/>, displayed by <see cref="WorkflowPage"/>.
/// </summary>
internal class WorkflowVM : ScopedVM
{
    public AutomationWorkflow Workflow => (AutomationWorkflow)Element;

    public WorkflowVM(AutomationWorkflow workflow) : base(workflow)
    {
    }
}
