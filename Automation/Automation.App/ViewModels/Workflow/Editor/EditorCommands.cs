using System.ComponentModel;
using System.Windows.Input;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class EditorSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsGridActivated { get; set; }
        public bool IsReadOnly { get; set; }
    }



    public class EditorCommands
    {
        public ICommand ZoomIn { get; private set; }
        public ICommand ZoomOut { get; private set; }
        public ICommand ZoomFit { get; private set; }

        public ICommand Add { get; private set; }
        public ICommand Delete { get; private set; }
        public ICommand Group { get; private set; }

        public ICommand HistoryPrevious { get; private set; }
        public ICommand HistoryNext { get; private set; }
    }
}
