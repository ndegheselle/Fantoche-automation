using System.ComponentModel;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class WorkflowEditorSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsGridActivated { get; set; }
        public bool IsReadOnly { get; set; }
    }
}
