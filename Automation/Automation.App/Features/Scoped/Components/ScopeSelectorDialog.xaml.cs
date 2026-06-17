using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Automation.App.Features.Scoped.Components;

public partial class ScopeSelectorDialog : UserControl
{
    public ScopeSelectorDialog()
    {
        InitializeComponent();

        // Tunnel so the selection is updated before the context menu opens and reads command states.
        TreeContainer.PreviewMouseRightButtonDown += OnPreviewRightButtonDown;
        // Commit the inline rename when its box loses focus (e.g. clicking elsewhere).
        ScopeTree.AddHandler(LostFocusEvent, new RoutedEventHandler(OnEditBoxLostFocus));
        ScopeTree.MouseDoubleClick += OnTreeDoubleClick;
    }

    private ScopeSelectorVm? Vm => DataContext as ScopeSelectorVm;

    /// <summary>
    /// WPF's <see cref="TreeView.SelectedItem"/> is read-only and cannot be bound two-way, so the
    /// selection is pushed onto the view model here instead.
    /// </summary>
    private void OnTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (Vm is not null)
            Vm.SelectedNode = e.NewValue as ScopeTreeNode;
    }

    /// <summary>
    /// Right-clicking selects the node under the cursor, or clears the selection when the click
    /// lands on empty space, so the context menu's "New scope" targets the right parent (root included).
    /// </summary>
    private void OnPreviewRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm is null)
            return;

        var container = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
        if (container is not null)
        {
            container.IsSelected = true;
        }
        else
        {
            // Empty space: clear the current selection.
            var selected = FindContainer(ScopeTree, Vm.SelectedNode);
            if (selected is not null)
                selected.IsSelected = false;
            Vm.SelectedNode = null;
        }
    }

    private void OnTreeDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var container = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
        (container?.DataContext as ScopeTreeNode)?.BeginRename();
    }

    private void OnEditBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TextBox { DataContext: ScopeTreeNode { IsEditing: true } node }
            && node.CommitEditCommand.CanExecute(null))
        {
            node.CommitEditCommand.Execute(null);
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>Resolves the <see cref="TreeViewItem"/> container hosting <paramref name="node"/>.</summary>
    private static TreeViewItem? FindContainer(ItemsControl parent, ScopeTreeNode? node)
    {
        if (parent is null || node is null)
            return null;

        if (parent.ItemContainerGenerator.ContainerFromItem(node) is TreeViewItem direct)
            return direct;

        foreach (var item in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem child)
            {
                var found = FindContainer(child, node);
                if (found is not null)
                    return found;
            }
        }
        return null;
    }
}
