using System.Threading.Tasks;
using Automation.App.Features.Packages.Components;
using Automation.App.Services;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

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
    [NotifyPropertyChangedFor(nameof(WithoutTarget))]
    private PackageClassTarget? _target;

    /// <summary>True when no target is set, used to toggle the empty placeholder.</summary>
    public bool WithoutTarget => Target == null;

    partial void OnTargetChanged(PackageClassTarget? value)
    {
        Task.Target = value;
        _ = _scopedService.EditAsync(Element);
    }

    [RelayCommand]
    private void SelectTarget()
    {
        var pickerVm = new TaskTargetPickerVm(ServiceProvider.Packages);
        ServiceProvider.Dialogs
            .CreateDialog(pickerVm)
            .WithSuccessCallback(() => ApplySelectedTarget(pickerVm))
            .Dismissible()
            .Show();
    }

    private void ApplySelectedTarget(TaskTargetPickerVm pickerVm)
    {
        if (pickerVm.SelectedTarget == null)
            return;

        Target = pickerVm.SelectedTarget;
    }

    [RelayCommand]
    private void RemoveTarget() => Target = null;
}
