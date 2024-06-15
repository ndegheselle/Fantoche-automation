using Automation.App.ViewModels.Scopes;
using System.ComponentModel;
using System.Windows;

namespace Automation.App.ViewModels.Graph
{
    public class EditorViewModel
    {
        public WorkflowScope WorkflowScope { get; set; }
    }

    public class ElementEndpoint : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; }
        public Point Anchor { get; set; }
        public bool IsConnected { get; set; }
    }

    public class ElementLink : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ElementLink(ElementEndpoint source, ElementEndpoint target)
        {
            Source = source;
            Target = target;

            Source.IsConnected = true;
            Target.IsConnected = true;
        }

        public ElementEndpoint Source { get; set; }
        public ElementEndpoint Target { get; set; }
    }
}
