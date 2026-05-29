using Automation.Shared.Data.Scoped;

namespace Automation.App.ViewModels.Scoped
{
    /// <summary>Wraps a domain <see cref="ScopedElement"/> in the matching view-model.</summary>
    public static class ScopedElementViewModelFactory
    {
        public static ScopedElementViewModel Create(ScopedElement model) => model switch
        {
            Scope scope => new ScopeViewModel(scope),
            AutomationWorkflow workflow => new WorkflowViewModel(workflow),
            BaseAutomationTask task => new TaskViewModel(task),
            _ => throw new NotSupportedException($"No view-model for scoped element type '{model.GetType().Name}'.")
        };
    }
}
