using Automation.Shared.Data.Scoped;

namespace Automation.App.ViewModels.Scoped
{
    /// <summary>UI wrapper for a task (and controls). Workflows derive from this (see WorkflowViewModel).</summary>
    public partial class TaskViewModel : ScopedElementViewModel
    {
        public new BaseAutomationTask Model => (BaseAutomationTask)base.Model;

        public TaskViewModel(BaseAutomationTask model) : base(model)
        {
        }
    }
}
