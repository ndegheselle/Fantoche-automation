using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
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

        [JsonIgnore]
        public bool IsExpanded { get; set; }

        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;

                // Since we are using a treeview, we need to expand the parent when a child is selected (otherwise the selection will not go through)
                if (value)
                {
                    ExpandParent();
                    IsExpanded = true;
                }

                OnPropertyChanged();
            }
        }

        public void ExpandParent()
        {
            if (Parent == null)
                return;

            Parent.ExpandParent();
            Parent.IsExpanded = true;
        }
    }

    public partial class Scope
    {
        [JsonIgnore]
        public ListCollectionView SortedChildrens { get; set; }
    }

    public partial class TaskNode
    {
        public EnumTaskNodeConnectorsOptions ConnectorsOptions { get; set; } = EnumTaskNodeConnectorsOptions.None;
    }

    [Flags]
    public enum EnumTaskNodeConnectorsOptions
    {
        None = 0,
        EditInputs = 1,
        EditOutputs = 2,
    }

    public enum EnumNodeConnectorType
    {
        Data,
        Flow
    }

    public partial class NodeConnector : INotifyPropertyChanged
    {
        public EnumNodeConnectorType Type { get; set; } = EnumNodeConnectorType.Data;

        [JsonIgnore]
        public bool IsConnected { get; set; }
        [JsonIgnore]
        public Point Anchor { get; set; }
    }

    public enum EnumNodeConnectionType
    {
        Data,
        Flow
    }

    public partial class NodeConnection
    {
        public EnumNodeConnectionType Type { get; set; } = EnumNodeConnectionType.Data;
    }
}
