using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Scoped.Tasks;

/// <summary>
/// View model wrapping an <see cref="AutomationTask"/>, displayed by <see cref="TaskPage"/>.
/// </summary>
internal partial class TaskVm : ScopedVm
{
    public AutomationTask Task => (AutomationTask)Element;

    public TaskVm(AutomationTask task) : base(task)
    {
        _target = task.Target;
    }

    /// <summary>
    /// Current task target. This is the observable source of truth the view binds to; the
    /// change is written back onto <see cref="AutomationTask.Target"/> in <see cref="OnTargetChanged"/>.
    /// </summary>
    [ObservableProperty]
    private PackageClassTarget? _target;

    partial void OnTargetChanged(PackageClassTarget? value)
    {
        Task.Target = value;
        _ = _scopedService.EditAsync(Element);
    }
}
