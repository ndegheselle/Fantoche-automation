using Automation.Models.Work;
using Joufflu.Popups;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Workflows.Editor.Components
{
    /// <summary>
    /// Logique d'interaction pour TaskInputSettingModal.xaml
    /// </summary>
    public partial class WorkflowSchemaModal : UserControl, IModalContent
    {
        public ModalOptions Options { get; private set; } = new ModalOptions();
        public IModal? ParentLayout { get; set; }

        public AutomationWorkflow Workflow { get; private set; }

        public string WorkflowSchemaSample { get; set; }

        public ICustomCommand CancelCommand { get; private set; }
        public ICustomCommand ValidateCommand { get; private set; }

        private IAlert _alert => this.GetCurrentAlert();
        private readonly string? _originalSchema;

        public WorkflowSchemaModal(AutomationWorkflow workflow) {
            Workflow = workflow;
            _originalSchema = Workflow.InputSchemaJson;

            if (string.IsNullOrEmpty(Workflow.InputSchemaJson))
                Workflow.InputSchema = new NJsonSchema.JsonSchema();
            WorkflowSchemaSample = Workflow.InputSchema!.ToSampleJson().ToString();

            Options.Title = $"{Workflow.Metadata.Name} - inputs schema";
            CancelCommand = new DelegateCommand(Cancel);
            ValidateCommand = new DelegateCommand(Validate, () => string.IsNullOrEmpty(WorkflowSchemaSample) == false);
            InitializeComponent();
        }

        public void Cancel()
        {
            Workflow.InputSchemaJson = _originalSchema;
            ParentLayout?.Hide();
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(WorkflowSchemaSample))
                return;

            try
            {
                Workflow.InputSchema = NJsonSchema.JsonSchema.FromSampleJson(WorkflowSchemaSample);
                // Update all start task InputSchemaJson
                var startTasks = Workflow.Graph.Nodes.OfType<GraphControl>().Where(x => x.TaskId == AutomationControl.StartTaskId);
                foreach(var task in startTasks)
                {
                    task.InputSchemaJson = Workflow.InputSchemaJson;
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

        #region UI events
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateCommand.RaiseCanExecuteChanged();
        }
        #endregion
    }
}