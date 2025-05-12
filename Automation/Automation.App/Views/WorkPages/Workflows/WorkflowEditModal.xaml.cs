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
        
        private readonly TasksClient _taskClient;

        public AutomationWorkflow NewWorkflow { get; set; }

        public WorkflowCreateModal(AutomationWorkflow newWorkflow) : base("Create new workflow")
        {
            _taskClient = Services.Provider.GetRequiredService<TasksClient>();
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
        public AutomationWorkflow Workflow { get; set; }

        public IModal? ParentLayout { get; set; }

        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Edit workflow" };

        public WorkflowEditModal(AutomationWorkflow workflow)
        {
            Workflow = workflow;
            InitializeComponent();
        }
    }
}
