using Automation.App.Features.Scoped.Workflows.Editor;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Scoped.Workflows;

/// <summary>
/// View model wrapping an <see cref="AutomationWorkflow"/>, displayed by <see cref="WorkflowPage"/>.
/// </summary>
internal class WorkflowVm : ScopedVm
{
    public AutomationWorkflow Workflow => (AutomationWorkflow)Element;

    public EditorVm Editor { get; private set; }

    public WorkflowVm(AutomationWorkflow workflow) : base(workflow)
    {
        Editor = new EditorVm();
    }
}
