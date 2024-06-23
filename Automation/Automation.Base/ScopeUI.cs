using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

// XXX : these may move to a UI specific class (add to add WPF specific dependencies in this project ...)
// Can make a wrapper, inherit from Node with mapping (NodeUI ?) or use a Converter and a dictionnary
namespace Automation.Base
{
    public partial class Node : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsExpanded { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;

                // Since we are using a treeview, we need to expand the parent when a child is selected (otherwise the selection will not go through)
                if (value)
                    ExpandParent();
                OnPropertyChanged();
            }
        }

        public void ExpandParent()
        {
            if (Parent == null)
                return;

            Parent.IsExpanded = true;
            Parent.ExpandParent();
        }
    }

    public partial class Scope
    {
        public ListCollectionView SortedChildrens { get; set; }
    }

    public partial class TaskNode
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public Point Location { get; set; }
    }

    public partial class NodeConnector : INotifyPropertyChanged
    {
        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }
    }

    public partial class NodeConnection
    {
        public NodeConnection(NodeConnector source, NodeConnector target)
        {
            Source = source;
            Target = target;

            source.IsConnected = true;
            target.IsConnected = true;
        }
    }
}
