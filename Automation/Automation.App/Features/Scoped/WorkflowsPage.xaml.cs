using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Features.Scoped;

public partial class WorkflowsPage : UserControl
{
    public WorkflowsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// WPF's <see cref="TreeView.SelectedItem"/> is read-only and cannot be bound two-way, so the
    /// selection is pushed onto the view model here instead.
    /// </summary>
    private void OnTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is WorkflowsPageVm vm)
            vm.Selected = e.NewValue as ScopedVm;
    }
}
