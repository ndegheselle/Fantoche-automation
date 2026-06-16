using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Automation.App.Features.Scoped.Components;

public partial class ScopeSelectorDialog : UserControl
{
    public ScopeSelectorDialog()
    {
        InitializeComponent();

        // Tunnel so the selection is updated before the context menu opens and reads command states.
        TreeContainer.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        // Commit the inline rename when its box loses focus (e.g. clicking elsewhere).
        ScopeTree.AddHandler(LostFocusEvent, OnEditBoxLostFocus, RoutingStrategies.Bubble);
        ScopeTree.DoubleTapped += OnTreeDoubleTapped;
    }

    private ScopeSelectorVm? Vm => DataContext as ScopeSelectorVm;

    /// <summary>
    /// Right-clicking selects the node under the cursor, or clears the selection when the click
    /// lands on empty space, so the context menu's "New scope" targets the right parent (root included).
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null || !e.GetCurrentPoint(TreeContainer).Properties.IsRightButtonPressed)
            return;

        var node = (e.Source as Visual)?
            .FindAncestorOfType<TreeViewItem>(includeSelf: true)?
            .DataContext as ScopeTreeNode;

        Vm.SelectedNode = node;
    }

    private void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        var node = (e.Source as Visual)?
            .FindAncestorOfType<TreeViewItem>(includeSelf: true)?
            .DataContext as ScopeTreeNode;

        node?.BeginRename();
    }

    private void OnEditBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox { DataContext: ScopeTreeNode { IsEditing: true } node }
            && node.CommitEditCommand.CanExecute(null))
        {
            node.CommitEditCommand.Execute(null);
        }
    }
}
