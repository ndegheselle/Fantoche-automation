using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.App.ViewModels;
using Joufflu.Popups;
using Joufflu.Shared.Layouts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl, IPage
    {
        public ILayout? ParentLayout { get; set; }
        public EditorViewModel? Editor { get; set; } = null;
        public WorkflowNode Workflow { get; set; }

        private readonly App _app = App.Current;
        private readonly TasksClient _client;
        private IModal _modal => this.GetCurrentModalContainer();

        public WorkflowPage(Guid workflowId)
        {
            _client = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Workflow = new WorkflowNode() { Id = workflowId };
            InitializeComponent();
            LoadFullWokflow(workflowId);
        }

        public async void LoadFullWokflow(Guid workflowId)
        {
            WorkflowNode? fullWorkflow = await _client.GetByIdAsync(workflowId) as WorkflowNode;

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
                await _client.UpdateAsync(Workflow.Id, Workflow);
        }
        #endregion
    }
}
