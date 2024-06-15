using Automation.App.ViewModels.Scopes;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl
    {
        public WorkflowPage(WorkflowScope scope)
        {
            InitializeComponent();
            this.DataContext = scope;
        }
    }
}
