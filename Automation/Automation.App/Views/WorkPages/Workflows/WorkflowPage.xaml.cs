using Automation.App.Shared.ApiClients;
using Automation.Models;
using Automation.App.ViewModels;
using Joufflu.Popups;
using Joufflu.Shared.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl, IPage
    {
        public ILayout? ParentLayout { get; set; }
        public Automation.Shared.Data.Task.AutomationWorkflow Workflow { get; set; }

        
        private readonly TasksClient _client;
        private IModal _modal => this.GetCurrentModal();

        public WorkflowPage(Automation.Shared.Data.Task.AutomationWorkflow workflow)
        {
            _client = Services.Provider.GetRequiredService<TasksClient>();
            Workflow = workflow;
            InitializeComponent();
            LoadFullWokflow(Workflow.Id);
        }

        public async void LoadFullWokflow(Guid workflowId)
        {
            Automation.Shared.Data.Task.AutomationWorkflow? fullWorkflow = await _client.GetByIdAsync(workflowId) as Automation.Shared.Data.Task.AutomationWorkflow;

            if (fullWorkflow == null)
                throw new ArgumentException("Workflow not found");
            Workflow = fullWorkflow;
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
