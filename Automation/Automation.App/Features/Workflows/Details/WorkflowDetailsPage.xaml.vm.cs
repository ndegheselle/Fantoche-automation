using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Automation.App.Features.Workflows.Editor;
using Automation.App.Features.Workflows.Editor.History;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.Input;
using NJsonSchema;

namespace Automation.App.Features.Workflows.Details
{
    public partial class WorkflowDetailsViewModel : ScopedDetailsViewModel<AutomationWorkflow>
    {
        public AutomationWorkflow Workflow => Element;

        /// <summary>
        /// Graph of the workflow, edited by the editor tab.
        /// </summary>
        public WorkflowEditorViewModel Editor { get; }

        /// <summary>
        /// What the workflow expects to be started with : the one schema of a graph written by hand,
        /// every other one being deduced from the mappings when it is saved.
        /// </summary>
        public string? InputSchemaJson
        {
            get => Workflow.InputSchemaJson;
            set
            {
                Workflow.InputSchemaJson = NullIfEmpty(value);
                OnPropertyChanged();
                MarkChanged();
                RefreshInput();
            }
        }

        /// <summary>
        /// What the start hands over for the values the caller doesn't give. Null when the graph has
        /// no start yet, there is then nothing to hold them.
        /// </summary>
        public string? InputDefaultsJson
        {
            get => Start?.InputMappingJson;
            set
            {
                if (Start == null)
                    return;

                Start.InputMappingJson = NullIfEmpty(value);
                OnPropertyChanged();
                MarkChanged();
                RefreshInput();
            }
        }

        public bool HasStart => Start != null;

        /// <summary>
        /// What is wrong with the input of the workflow : the schema isn't one, or the default
        /// values don't match it.
        /// </summary>
        public ObservableCollection<string> InputErrors { get; } = [];

        public bool HasInputErrors => InputErrors.Count > 0;

        /// <summary>
        /// The start of the graph, which is the node handing the input over. A workflow holds a
        /// single one (see <see cref="TasksGraph.GetStructureErrors"/>).
        /// </summary>
        private GraphControl? Start => Workflow.Graph.GetStartNodes().FirstOrDefault();

        public bool StopAtFirstEnd
        {
            get => Workflow.WorkflowSettings.StopAtFirstEnd;
            set => SetSetting(value, v => Workflow.WorkflowSettings.StopAtFirstEnd = v);
        }

        public bool StopIfAnyTaskFail
        {
            get => Workflow.WorkflowSettings.StopIfAnyTaskFail;
            set => SetSetting(value, v => Workflow.WorkflowSettings.StopIfAnyTaskFail = v);
        }

        public WorkflowDetailsViewModel(ScopedNode node, WorkflowsViewModel parent) : base(node, parent)
        {
            Editor = new WorkflowEditorViewModel(Workflow, SaveGraphCommand, () => CurrentTab = EnumDetailTab.Settings);
            RefreshInput();
            Editor.History.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(EditorHistory.HasUnsavedChanges))
                    SaveGraphCommand.NotifyCanExecuteChanged();
            };
        }

        /// <summary>
        /// Check what the input of the workflow is made of : its schema has to be one, and the
        /// default values have to match it. They only stand for what the caller doesn't give, so a
        /// value the schema requires may be missing from them.
        /// </summary>
        private void RefreshInput()
        {
            InputErrors.Clear();

            JsonSchema? schema = null;
            if (!string.IsNullOrWhiteSpace(Workflow.InputSchemaJson))
            {
                try
                {
                    schema = JsonSchema.FromJsonAsync(Workflow.InputSchemaJson).Result;
                }
                catch (Exception exception)
                {
                    InputErrors.Add($"Input schema : {exception.Message}");
                }
            }

            if (Start != null && schema != null)
            {
                List<string> errors = Workflow.Sample().Validate(
                    Start,
                    Start.InputMappingJson,
                    schema,
                    partial: true);

                foreach (string error in errors)
                    InputErrors.Add($"Default values : {error}");
            }

            OnPropertyChanged(nameof(HasInputErrors));
        }

        private static string? NullIfEmpty(string? json) => string.IsNullOrWhiteSpace(json) ? null : json;

        /// <summary>
        /// Save of the graph, kept apart from the general infos one so each is only enabled by the
        /// changes it is about.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveGraph))]
        private Task SaveGraph() => SaveElementAsync($"The graph of the workflow '{Node.Name}' has been saved.");

        private bool CanSaveGraph => Editor.History.HasUnsavedChanges;

        /// <summary>
        /// The graph having been persisted, its history has nothing left to save.
        /// </summary>
        protected override void OnSaved() => Editor.History.MarkSaved();

        /// <summary>
        /// Store a setting on the workflow, the settings being held by the element itself rather than
        /// by observable properties.
        /// </summary>
        private void SetSetting(bool value, Action<bool> set, [CallerMemberName] string? propertyName = null)
        {
            set(value);
            OnPropertyChanged(propertyName);
            MarkChanged();
        }
    }
}
