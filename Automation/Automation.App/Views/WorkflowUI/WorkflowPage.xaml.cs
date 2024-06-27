using Automation.App.ViewModels.Graph;
using Automation.Base;
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

        public WorkflowPage(WorkflowNode workflow)
        {
            // Load full workflow
            ScopeRepository scopeRepository = new ScopeRepository();
            workflow = (WorkflowNode)scopeRepository.GetNode(workflow.Id);

            Editor = new EditorViewModel(workflow);
            this.DataContext = this;
            InitializeComponent();
        }
    }
}
