using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Data;

namespace Automation.Base.ViewModels
{
    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Task,
        Workflow,
    }

    [JsonDerivedType(typeof(ScopedNode), typeDiscriminator: "node")]
    [JsonDerivedType(typeof(Scope), typeDiscriminator: "scope")]
    public class ScopedElement : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ParentId { get; set; }

        [JsonIgnore]
        public Scope? Parent { get; set; }

        public string Name { get; set; }

        public EnumScopedType Type { get; set; }

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

                // Since we are using a treeview, we need to expand the parent when
                // a child is selected (otherwise the selection will not go through)
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

    public class ScopedNode : ScopedElement
    {
        public Node Node { get; set; }

        public ScopedNode(Node node)
        {
            Node = node;
            if (node is TaskNode task)
                Type = EnumScopedType.Task;
            else if (node is WorkflowNode workflow)
                Type = EnumScopedType.Workflow;
        }
    }

    public class Scope : ScopedElement
    {
        [JsonIgnore]
        public ObservableCollection<ScopedElement> Childrens { get; set; } = [];
        [JsonIgnore]
        public ListCollectionView SortedChildrens { get; set; }

        public Scope()
        {
            Type = EnumScopedType.Scope;
            SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Type), ListSortDirection.Ascending));
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
        }

        public void AddChild(ScopedElement element)
        {
            element.Parent = this;
            Childrens.Add(element);
        }
    }
}
