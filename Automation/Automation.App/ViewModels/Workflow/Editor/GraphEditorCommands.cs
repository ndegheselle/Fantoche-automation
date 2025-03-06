using Automation.App.Shared.ViewModels.Work;
using Automation.App.ViewModels.Workflow.Editor.Actions;
using Automation.App.Views.WorkPages.Workflows.Editor;
using Automation.Shared.Data;
using System.Windows.Input;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorCommands
    {
        private readonly GraphEditorViewModel _editor;
        private readonly GraphEditorActions _actions;

        public ICommand ZoomIn { get; private set; }

        public ICommand ZoomOut { get; private set; }

        public ICommand ZoomFit { get; private set; }

        public ICommand DeleteSelection { get; private set; }

        public ICommand GroupSelection { get; private set; }

        public ICustomCommand HistoryUndo { get; private set; }

        public ICustomCommand HistoryRedo { get; private set; }

        public ICommand Save { get; private set; }

        public ICommand DisconnectConnectorCommand { get; private set; }

        public GraphEditorCommands(GraphEditorViewModel editor)
        {
            _editor = editor;
            _actions = _editor.Actions;

            // XXX : use EditorCommands.ZoomIn ?
            ZoomIn = new DelegateCommand(_editor.Canvas.NodifyEditorElement.ZoomIn);
            ZoomOut = new DelegateCommand(_editor.Canvas.NodifyEditorElement.ZoomOut);
            ZoomFit = new DelegateCommand(() => _editor.Canvas.NodifyEditorElement.FitToScreen());

            _actions.PropertyChanged += Actions_PropertyChanged;

            HistoryUndo = new DelegateCommand(_actions.Undo, () => _actions.IsUndoAvailable);
            HistoryRedo = new DelegateCommand(_actions.Redo, () => _actions.IsRedoAvailable);

            DisconnectConnectorCommand = new DelegateCommand<TaskConnector>(
                (connector) => _actions.Execute(new ConnectionsRemoveAction(_editor.GetLinkedConnections(connector))));
        }

        private void Actions_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GraphEditorActions.IsUndoAvailable))
                HistoryUndo.RaiseCanExecuteChanged();
            if (e.PropertyName == nameof(GraphEditorActions.IsRedoAvailable))
                HistoryRedo.RaiseCanExecuteChanged();
        }
    }
}
