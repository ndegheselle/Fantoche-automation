using System.Windows;
using System.Windows.Controls;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.Input;
using Joufflu;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour WorkflowEditor.xaml
    /// </summary>
    public partial class WorkflowEditor : UserControl
    {
        public WorkflowEditorViewModel? ViewModel => DataContext as WorkflowEditorViewModel;

        /// <summary>
        /// Drop of a node dragged out of the tree, added to the graph where it landed. Held by the
        /// view rather than by the view model : turning the drop position into graph coordinates
        /// needs the editor and its viewport.
        /// </summary>
        public IRelayCommand<IDataObject> DropCommand { get; }

        public WorkflowEditor()
        {
            InitializeComponent();
            DropCommand = new RelayCommand<IDataObject>(OnDrop, CanDrop);
        }

        private void OnDrop(IDataObject? data)
        {
            if (GetTask(data) is not BaseAutomationTask task || data is not DropData drop)
                return;

            // The drop is placed on the border holding the target, which the editor translates to
            // the graph itself.
            ViewModel?.Add(task, Editor.GetLocationInsideEditor(drop.Position, drop.Target));
        }

        /// <summary>
        /// Only a task or a workflow can be dropped, a scope having nothing to run, and the workflow
        /// being edited can't contain itself.
        /// </summary>
        private bool CanDrop(IDataObject? data)
        {
            BaseAutomationTask? task = GetTask(data);
            return task != null && task.Id != ViewModel?.Workflow.Id;
        }

        /// <summary>
        /// What the dragged node is worth to the editor, <see langword="null"/> for anything else
        /// than a task or a workflow coming from the tree.
        /// </summary>
        private static BaseAutomationTask? GetTask(IDataObject? data)
            => (data?.GetData(typeof(ScopedNode)) as ScopedNode)?.TaskElement;
    }
}
