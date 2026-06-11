using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Scoped.Tasks;

/// <summary>
/// View model wrapping an <see cref="AutomationTask"/>, displayed by <see cref="TaskPage"/>.
/// </summary>
internal class TaskVm : ScopedVm
{
    public AutomationTask Task => (AutomationTask)Element;

    public TaskVm(AutomationTask task) : base(task)
    {
    }
}
