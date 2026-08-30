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
        /// Node the tree selected on a press, kept aside until the button is released : a press is
        /// also how a drag starts, and showing the details of what is being dragged would take the
        /// editor it is dragged to away from under the pointer.
        /// </summary>
        private ScopedNode? _pressedNode;

        public WorkflowsPage()
        {
            InitializeComponent();
            this.Loaded += (_, __) => _ = ViewModel.RefreshAsync();
        }

        private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ViewModel == null)
                return;

            var node = e.NewValue as ScopedNode;
            // A selection that doesn't come from a press (keyboard, code) is shown right away.
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                ViewModel.Selected = node;
                return;
            }

            _pressedNode = node;
        }

        /// <summary>
        /// The press was a click : what it selected is opened, as if the tree had been followed
        /// directly.
        /// </summary>
        private void OnTreeMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null || _pressedNode == null)
                return;

            ViewModel.Selected = _pressedNode;
            _pressedNode = null;
        }

        /// <summary>
        /// The press turned into a drag : the details stay on what is open (the drag is likely
        /// aimed at it) and the tree goes back to highlighting it, no button up being raised once
        /// the system took the drag over.
        /// </summary>
        private void OnTreeQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (ViewModel == null || _pressedNode == null)
                return;

            _pressedNode = null;
            if (ViewModel.Selected != null)
                ViewModel.Selected.IsSelected = true;
        }
    }
}
