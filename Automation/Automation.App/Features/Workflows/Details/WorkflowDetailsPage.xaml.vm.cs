using Automation.App.Features.Workflows.Editor;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Workflows.Details
{
    public class WorkflowDetailsViewModel : ScopedDetailsViewModel<AutomationWorkflow>
    {
        public AutomationWorkflow Workflow => Element;

        /// <summary>
        /// Graph of the workflow, edited by the editor tab.
        /// </summary>
        public WorkflowEditorViewModel Editor { get; }

        protected override string TypeName => "workflow";

        public WorkflowDetailsViewModel(ScopedNode node, WorkflowsViewModel parent) : base(node, parent)
        {
            Editor = new WorkflowEditorViewModel(Workflow, SaveCommand);
        }

        /// <summary>
        /// The graph having been persisted, its history has nothing left to save.
        /// </summary>
        protected override void OnSaved() => Editor.History.MarkSaved();
    }
}
