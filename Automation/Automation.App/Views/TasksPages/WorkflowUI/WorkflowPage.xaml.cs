using Automation.App.ViewModels.Tasks;
using Automation.Shared.Data;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public EditorViewModel? Editor { get; set; } = null;
        public WorkflowScopedItem Workflow { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly ITaskClient _client;

        public WorkflowPage(WorkflowScopedItem workflow)
        {
            _client = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            InitializeComponent();
            LoadWokflow(workflow);
        }

        public async void LoadWokflow(WorkflowScopedItem workflow)
        {
            WorkflowNode? fullWorkflow = await _client.GetWorkflowAsync(workflow.WorkflowNode.Id);

            if (fullWorkflow == null)
                throw new ArgumentException("Workflow not found");

            Workflow.WorkflowNode = fullWorkflow;
            Editor = new EditorViewModel(fullWorkflow);
            this.DataContext = Workflow;
        }
    }
}
