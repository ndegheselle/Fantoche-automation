using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Workflows.Details
{
    public class WorkflowDetailsViewModel : ScopedDetailsViewModel<AutomationWorkflow>
    {
        public AutomationWorkflow Workflow => Element;

        protected override string TypeName => "workflow";

        public WorkflowDetailsViewModel(ScopedNode node, WorkflowsViewModel parent) : base(node, parent)
        { }
    }
}
