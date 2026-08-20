using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Features.Workflows
{
    /// <summary>
    /// Logique d'interaction pour WorkflowsPage.xaml
    /// </summary>
    public partial class WorkflowsPage : UserControl
    {
        public WorkflowsViewModel ViewModel => (WorkflowsViewModel)this.DataContext;

        public WorkflowsPage()
        {
            InitializeComponent();
            this.Loaded += (_, __) => _ = ViewModel.RefreshAsync();
        }

        private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ViewModel == null)
                return;

            ViewModel.Selected = e.NewValue as ScopedNode;
        }
    }
}
