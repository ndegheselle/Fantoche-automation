using Automation.Models.Work;
using Joufflu.Popups;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components
{
    /// <summary>
    /// Logique d'interaction pour TaskInputSettingModal.xaml
    /// </summary>
    public partial class WorkflowInputModal : UserControl, IModalContent
    {
        public ModalOptions Options { get; private set; } = new ModalOptions();
        public IModal? ParentLayout { get; set; }

        public AutomationWorkflow Workflow { get; private set; }

        public string InputSampleJson { get; private set; }
        
        public ICustomCommand CancelCommand { get; private set; }
        public ICustomCommand ValidateCommand { get; private set; }

        private IAlert _alert => this.GetCurrentAlert();
        private readonly string? _originalSchema;

        public WorkflowInputModal(AutomationWorkflow workflow) {
            Workflow = workflow;
            
            _originalSchema = Workflow.InputSchemaJson;
            InputSampleJson = Workflow.InputSchema?.ToSampleJson().ToString() ?? "";
            
            Options.Title = $"{Workflow.Metadata.Name} - input schema";
            CancelCommand = new DelegateCommand(Cancel);
            ValidateCommand = new DelegateCommand(Validate);
            InitializeComponent();
        }

        private void Cancel()
        {
            Workflow.InputSchemaJson = _originalSchema;
            ParentLayout?.Hide();
        }

        private void Validate()
        {
            try
            {
                // Update all start task InputSchemaJson
                var startTasks = Workflow.Graph.Nodes.OfType<GraphControl>().Where(x => x.TaskId == AutomationControl.StartTaskId);
                foreach(var task in startTasks)
                {
                    task.InputSchemaJson = Workflow.InputSchemaJson;
                    task.OutputSchemaJson = Workflow.InputSchemaJson;
                }
            }
            catch
            {
                _alert.Error("The sample can't be converted to a Schema.");
                return;
            }

            _alert.Success("Workflow input schema changed !");
            ParentLayout?.Hide(true);
        }
    }
}