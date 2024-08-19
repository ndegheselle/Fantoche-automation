using Automation.App.Base;
using Automation.App.ViewModels;
using Automation.App.ViewModels.Tasks;
using Automation.App.Views.TasksPages.TaskUI;
using Automation.Shared.Data;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows;
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
        private readonly IModalContainer _modal;

        public WorkflowPage(ScopedTaskItem workflow)
        {
            _modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            _client = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            Scoped = workflow;
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

        #region UI Events
        private async void ButtonParameters_Click(object sender, RoutedEventArgs e)
        {
            if (Workflow == null)
                return;
            if (await _modal.Show(new WorkflowEditModal(Workflow)))
                Workflow = await _client.UpdateWorkflowAsync(Workflow);
        }
        #endregion
    }
}
