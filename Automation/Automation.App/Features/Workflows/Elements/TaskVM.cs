using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Workflows.Elements;

/// <summary>
/// View model wrapping an <see cref="AutomationTask"/>, displayed by <see cref="TaskPage"/>.
/// </summary>
internal class TaskVM : ScopedVM
{
    public AutomationTask Task => (AutomationTask)Element;

    public TaskVM(AutomationTask task) : base(task)
    {
    }
}
