using Automation.App.Features.Packages.Components;
using Automation.App.Services;
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
        WithoutTarget = Task.Target == null;
    }

    [ObservableProperty] private bool _withoutTarget;

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

        Task.Target = pickerVm.SelectedTarget;
        WithoutTarget = false;
        OnPropertyChanged(nameof(Task));
    }
}
