using System.Runtime.CompilerServices;
using Fantoche.App.Common;
using Fantoche.App.Features.Workflows.Editor;
using Fantoche.App.Features.Workflows.Editor.History;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Fantoche.App.Features.Workflows.Details
{
    public partial class WorkflowDetailsViewModel : ScopedDetailsViewModel<AutomationWorkflow>
    {
        public AutomationWorkflow Workflow => Element;

        /// <summary>Graph of the workflow, edited by the editor tab.</summary>
        public WorkflowEditorViewModel Editor { get; }

        public bool StopIfAnyTaskFail
        {
            get => Workflow.WorkflowSettings.StopIfAnyTaskFail;
            set => SetSetting(value, v => Workflow.WorkflowSettings.StopIfAnyTaskFail = v);
        }

        public bool RunOnWorker
        {
            get => Workflow.Settings.Execution == EnumExecutionMode.Remote;
            set => SetSetting(value, v => Workflow.Settings.Execution = v ? EnumExecutionMode.Remote : EnumExecutionMode.Local);
        }

        /// <summary>See <see cref="TaskSettings.StorageRetention"/>.</summary>
        public int StorageRetention
        {
            get => Workflow.Settings.StorageRetention;
            set
            {
                Workflow.Settings.StorageRetention = Math.Max(value, 0);
                OnPropertyChanged();
                MarkChanged();
            }
        }

        public WorkflowDetailsViewModel(
            ScopedNode node,
            WorkflowsViewModel parent,
            IScopedService scoped,
            IHistoryService history,
            IExecutionService execution,
            IOverlayer overlays,
            IToastService toasts)
            : base(node, parent, scoped, history, overlays, toasts)
        {
            Editor = new WorkflowEditorViewModel(Workflow, SaveGraphCommand, scoped, execution, history, toasts);
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

        /// <summary>The graph having been persisted, its history has nothing left to save.</summary>
        protected override void OnSaved() => Editor.History.MarkSaved();

        /// <summary>
        /// Store a setting on the workflow, which holds them itself rather than as observable
        /// properties.
        /// </summary>
        private void SetSetting(bool value, Action<bool> set, [CallerMemberName] string? propertyName = null)
        {
            set(value);
            OnPropertyChanged(propertyName);
            MarkChanged();
        }
    }
}
