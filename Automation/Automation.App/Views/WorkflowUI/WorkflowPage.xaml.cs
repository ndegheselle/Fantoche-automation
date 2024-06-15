using Automation.App.ViewModels.Graph;
using Automation.App.ViewModels.Scopes;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl
    {
        public EditorViewModel Editor { get; set; }

        public WorkflowPage(WorkflowScope scope)
        {
            Editor = new EditorViewModel(scope);
            InitializeComponent();
            this.DataContext = this;
        }
    }
}
