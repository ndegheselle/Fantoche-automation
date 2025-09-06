using Automation.App.Shared.ApiClients;
using Automation.Models.Schema;
using Automation.Models.Work;
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
        public AutomationWorkflow Workflow { get; set; }
        public SchemaObject Schema { get; set; }

        private readonly TasksClient _client;
        private IModal _modal => this.GetCurrentModal();

        public WorkflowPage(AutomationWorkflow workflow)
        {
            Schema = new SchemaObject();
            SchemaObject schemaObject = new SchemaObject().AddValue("afdlfljksdlf", 1).AddValue("kllkphgfopo", 1);
            Schema
                .AddValue("tata")
                .AddArray("arras")
                .Add(new SchemaObjectProperty("flsdkfl", schemaObject), 0)
                .AddValue("toto");

            _client = Services.Provider.GetRequiredService<TasksClient>();
            Workflow = workflow;
            InitializeComponent();
            LoadFullWokflow(Workflow.Id);
        }

        public async void LoadFullWokflow(Guid workflowId)
        {
            AutomationWorkflow? fullWorkflow = await _client.GetByIdAsync(workflowId) as AutomationWorkflow;

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
