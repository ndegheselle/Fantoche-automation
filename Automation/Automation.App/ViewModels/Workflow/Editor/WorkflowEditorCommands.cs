using System.ComponentModel;
using System.Windows.Input;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class WorkflowEditorCommands
    {
        private readonly WorkflowEditorViewModel _editor;

        public ICommand ZoomIn { get; private set; }
        public ICommand ZoomOut { get; private set; }
        public ICommand ZoomFit { get; private set; }

        public ICommand DeleteSelection { get; private set; }
        public ICommand GroupSelection { get; private set; }

        public ICommand HistoryPrevious { get; private set; }
        public ICommand HistoryNext { get; private set; }
        public ICommand Save { get; private set; }

        public WorkflowEditorCommands(WorkflowEditorViewModel editor)
        {
            _editor = editor;
        }
    }
}
