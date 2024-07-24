using Automation.App.ViewModels.Tasks;
using Automation.Shared.Data;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows.Controls;
using static System.Formats.Asn1.AsnWriter;

namespace Automation.App.Views.TasksPages.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public EditorViewModel? Editor { get; set; } = null;
        public ScopedTaskItem Scoped { get; set; }
        public WorkflowNode? Workflow { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly ITaskClient _client;

        public WorkflowPage(ScopedTaskItem workflow)
        {
            Scoped = workflow;
            _client = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            InitializeComponent();
            LoadWokflow(Scoped);
        }

        public async void LoadWokflow(ScopedTaskItem workflow)
        {
            WorkflowNode? fullWorkflow = await _client.GetWorkflowAsync(workflow.TargetId);

            if (fullWorkflow == null)
                throw new ArgumentException("Workflow not found");

            Workflow = fullWorkflow;
            Editor = new EditorViewModel(Workflow);
        }
    }
}
