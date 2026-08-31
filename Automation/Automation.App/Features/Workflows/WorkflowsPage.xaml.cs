using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Automation.App.Features.Workflows
{
    /// <summary>
    /// Logique d'interaction pour WorkflowsPage.xaml
    /// </summary>
    public partial class WorkflowsPage : UserControl
    {
        public WorkflowsViewModel ViewModel => (WorkflowsViewModel)this.DataContext;

        /// <summary>
        /// What a press selected, kept aside until the button is released : a press is also how a
        /// drag starts, and showing the details of what is being dragged would take the editor it
        /// is dragged to away from under the pointer.
        /// </summary>
        private Action? _pressedSelection;

        public WorkflowsPage()
        {
            InitializeComponent();
            this.Loaded += (_, __) => _ = ViewModel.RefreshAsync();
        }

        private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ViewModel == null)
                return;

            // The tree item is already selected, only the details have to follow.
            var node = e.NewValue as ScopedNode;
            Select(() => ViewModel.Selected = node);
        }

        /// <summary>
        /// A result is opened rather than only selected : it is a node of the tree, which reveals it
        /// at its place once the search is cleared.
        /// </summary>
        private void OnResultsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null || e.AddedItems.Count == 0)
                return;

            if (e.AddedItems[0] is ScopedNode node)
                Select(() => ViewModel.Open(node));
        }

        /// <summary>
        /// Follow a selection, or hold it until the press it comes from turns out to be a click
        /// rather than a drag.
        /// </summary>
        private void Select(Action apply)
        {
            // A selection that doesn't come from a press (keyboard, code) is followed right away.
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                apply();
                return;
            }

            _pressedSelection = apply;
        }

        /// <summary>
        /// The press was a click : the selection it made is followed, as if the tree or the results
        /// had been followed directly.
        /// </summary>
        private void OnSelectionMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Action? apply = _pressedSelection;
            _pressedSelection = null;
            apply?.Invoke();
        }

        /// <summary>
        /// The press turned into a drag : what is open stays open (the drag is likely aimed at it)
        /// and the lists go back to highlighting it, no button up being raised once the system took
        /// the drag over.
        /// </summary>
        private void OnSelectionQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (ViewModel == null || _pressedSelection == null)
                return;

            _pressedSelection = null;

            ScopedNode? open = ViewModel.Selected;
            if (open == null)
                return;

            open.IsSelected = true;
        }
    }
}
