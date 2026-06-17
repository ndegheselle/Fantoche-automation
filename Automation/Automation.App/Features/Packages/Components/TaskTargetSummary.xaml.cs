using System.Windows;
using System.Windows.Controls;
using Automation.App.Services;
using Automation.Shared.Data.Execution;

namespace Automation.App.Features.Packages.Components;

/// <summary>
/// Displays the <see cref="PackageClassTarget"/> of a task and lets the user pick one (through the
/// <see cref="TaskTargetPickerDialog"/>) or clear it. When no target is set, an empty placeholder
/// with a "Select" action is shown instead.
/// </summary>
public partial class TaskTargetSummary : UserControl
{
    /// <summary>
    /// The currently selected target, or <c>null</c> when none is set.
    /// </summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(PackageClassTarget),
            typeof(TaskTargetSummary),
            new FrameworkPropertyMetadata(default(PackageClassTarget), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public PackageClassTarget? Target
    {
        get => (PackageClassTarget?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public TaskTargetSummary()
    {
        InitializeComponent();
    }

    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        var pickerVm = new TaskTargetPickerVm(ServiceProvider.Packages);
        ServiceProvider.Dialogs
            .CreateDialog(pickerVm)
            .WithSuccessCallback(() =>
            {
                if (pickerVm.SelectedTarget != null)
                    Target = pickerVm.SelectedTarget;
            })
            .Dismissible()
            .Show();
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e) => Target = null;
}
