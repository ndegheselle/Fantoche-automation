using Automation.App.ViewModels.Graph;
using Automation.Shared.ViewModels;
using Automation.Supervisor.Repositories;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl
    {
        public EditorViewModel Editor { get; set; }

        public WorkflowPage(Guid id)
        {
            // Load full workflow
            ScopeRepository scopeRepository = new ScopeRepository();
            WorkflowNode workflow = (WorkflowNode)scopeRepository.GetNode(id);

            Editor = new EditorViewModel(workflow);
            this.DataContext = this;
            InitializeComponent();
        }
    }
}
