using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Base;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Workflows
{
    public class WorkflowCreateModal : TextBoxModal, IModalContent
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;

        public WorkflowNode NewWorkflow { get; set; }

        public WorkflowCreateModal(WorkflowNode newWorkflow) : base("Create new workflow")
        {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            NewWorkflow = newWorkflow;
            ValidateCommand = new DelegateCommand(Validate);
            BindValue(nameof(Scope.Name), NewWorkflow);
        }

        public async void Validate()
        {
            NewWorkflow.ClearErrors();
            try
            {
                NewWorkflow.Id = await _taskClient.CreateAsync(NewWorkflow);
                ParentLayout?.Hide(true);
            } catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    NewWorkflow.AddErrors(ex.Errors);
                return;
            }
        }
    }


    /// <summary>
    /// Logique d'interaction pour WorkflowEdit.xaml
    /// </summary>
    public partial class WorkflowEditModal : UserControl, IModalContent
    {
        public WorkflowNode Workflow { get; set; }

        public Modal? ParentLayout { get; set; }

        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Edit workflow" };

        public WorkflowEditModal(WorkflowNode workflow)
        {
            Workflow = workflow;
            InitializeComponent();
        }
    }
}
