using Automation.Shared.Data.Scoped;

namespace Automation.App.ViewModels.Scoped
{
    /// <summary>UI wrapper for a workflow. A workflow is a BaseAutomationTask, so this extends TaskViewModel.</summary>
    public partial class WorkflowViewModel : TaskViewModel
    {
        public new AutomationWorkflow Model => (AutomationWorkflow)base.Model;

        public WorkflowViewModel(AutomationWorkflow model) : base(model)
        {
        }
    }
}
