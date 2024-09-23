using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Data;
using Usuel.Shared;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public enum EnumScopedTabs
    {
        Default,
        History,
        Settings
    }

    [JsonDerivedType(typeof(TaskNode), "task")]
    [JsonDerivedType(typeof(WorkflowNode), "workflow")]
    [JsonDerivedType(typeof(Scope), "scope")]
    public class ScopedElement : ErrorValidationModel, INamed, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public EnumScopedType Type { get; set; }
        public EnumScopedTabs FocusOn { get; set; } = EnumScopedTabs.History;

        #region UI specific
        [JsonIgnore]
        public Scope? Parent { get; set; }
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

        #endregion
    }

    public class Scope : ScopedElement
    {
        public Guid? ParentId { get; set; }
        public Dictionary<string, string> Context { get; set; } = new Dictionary<string, string>();
        public ObservableCollection<ScopedElement> Childrens { get; set; }

        [JsonIgnore]
        public ListCollectionView? SortedChildrens { get; set; }

        public Scope()
        {
            Type = EnumScopedType.Scope;
            Childrens = [];
        }

        public void RefreshChildrens()
        {
            foreach (var child in Childrens)
                child.Parent = this;
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
