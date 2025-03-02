using Automation.App.Shared.ViewModels.Work;
using Automation.App.ViewModels.Workflow.Editor.Actions;
using Automation.App.Views.WorkPages.Workflows.Editor;
using Nodify;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorCommands
    {
        private readonly Graph _graph;
        private readonly GraphEditorActions _actions;
        private readonly GraphEditorCanvas _canvas;

        public ICommand ZoomIn { get; private set; }
        public ICommand ZoomOut { get; private set; }
        public ICommand ZoomFit { get; private set; }

        public ICommand DeleteSelection { get; private set; }
        public ICommand GroupSelection { get; private set; }

        public ICommand HistoryPrevious { get; private set; }
        public ICommand HistoryNext { get; private set; }
        public ICommand Save { get; private set; }

        public ICommand DisconnectConnectorCommand { get; private set; }

        public GraphEditorCommands(GraphEditorActions actions, GraphEditorCanvas canvas)
        {
            _actions = actions;
            _canvas = canvas;

            // XXX : use EditorCommands.ZoomIn ?
            ZoomIn = new DelegateCommand(_canvas.NodifyEditorElement.ZoomIn);
            ZoomOut = new DelegateCommand(_canvas.NodifyEditorElement.ZoomOut);
            ZoomFit = new DelegateCommand(() => _canvas.NodifyEditorElement.FitToScreen());

            DisconnectConnectorCommand = new DelegateCommand<TaskConnector>(_actions.DisconnectConnector);
        }
    }
}
