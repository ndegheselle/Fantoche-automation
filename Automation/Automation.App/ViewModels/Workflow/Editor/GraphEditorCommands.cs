using Automation.App.Shared.ViewModels.Work;
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
        private readonly GraphEditorViewModel _editor;

        public ICommand ZoomIn { get; private set; }
        public ICommand ZoomOut { get; private set; }
        public ICommand ZoomFit { get; private set; }

        public ICommand DeleteSelection { get; private set; }
        public ICommand GroupSelection { get; private set; }

        public ICommand HistoryPrevious { get; private set; }
        public ICommand HistoryNext { get; private set; }
        public ICommand Save { get; private set; }

        public ICommand DisconnectConnectorCommand { get; private set; }

        public GraphEditorCommands(GraphEditorViewModel editor)
        {
            _editor = editor;
            
            // XXX : use EditorCommands.ZoomIn ?
            ZoomIn = new DelegateCommand(_editor.Canvas.NodifyEditorElement.ZoomIn);
            ZoomOut = new DelegateCommand(_editor.Canvas.NodifyEditorElement.ZoomOut);
            ZoomFit = new DelegateCommand(() => _editor.Canvas.NodifyEditorElement.FitToScreen());

            DisconnectConnectorCommand = new DelegateCommand<TaskConnector>(_editor.DisconnectConnector);
        }
    }
}
