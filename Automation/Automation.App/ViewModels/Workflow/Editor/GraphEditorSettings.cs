using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsGridActivated { get; set; }
        public bool IsReadOnly { get; set; }
    }
}
