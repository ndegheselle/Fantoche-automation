using System.Runtime.CompilerServices;
using Automation.App.Features.Workflows.Editor;
using Automation.App.Features.Workflows.Editor.History;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Details
{
    public partial class WorkflowDetailsViewModel : ScopedDetailsViewModel<AutomationWorkflow>
    {
        public AutomationWorkflow Workflow => Element;

        /// <summary>
        /// Graph of the workflow, edited by the editor tab.
        /// </summary>
        public WorkflowEditorViewModel Editor { get; }

        protected override string TypeName => "workflow";

        public bool IsStoringAllData
        {
            get => Workflow.WorkflowSettings.IsStoringAllData;
            set => SetSetting(value, v => Workflow.WorkflowSettings.IsStoringAllData = v);
        }

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
            Editor = new WorkflowEditorViewModel(Workflow, SaveGraphCommand);
            Editor.History.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(EditorHistory.HasUnsavedChanges))
                    SaveGraphCommand.NotifyCanExecuteChanged();
            };
        }

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
