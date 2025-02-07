using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Data;
using Usuel.Shared;

namespace Automation.App.Shared.ViewModels.Work
{
    public enum EnumScopedTabs
    {
        Default,
        History,
        Settings
    }

    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    [JsonDerivedType(typeof(Scope), "scope")]
    public class ScopedElement : ErrorValidationModel, IScopedElement, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Guid> ParentTree { get; set; } = [];
        public Guid? ParentId { get; set; }

        public EnumScopedType Type { get; set; }
        public EnumScopedTabs FocusOn { get; set; } = EnumScopedTabs.History;

        #region UI specific
        [JsonIgnore]
        public Scope? Parent { get; set; }

        private bool _isExpanded = false;
        [JsonIgnore]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                if (value && !IsSelected)
                {
                    IsSelected = true;
                }
                NotifyPropertyChanged();
            }
        }

        private bool _isSelected = false;
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public void ExpandParent()
        {
            if (Parent == null)
                return;

            Parent.ExpandParent();
            Parent.IsExpanded = true;
        }

        public void ChangeParent(Scope scope)
        {
            Parent = scope;
            ParentTree = [..scope.ParentTree, scope.Id];
            ParentId = scope.Id;
        }
        #endregion
    }

    public class Scope : ScopedElement
    {
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
            {
                child.Parent = this;
                if (child is Scope subScope)
                    subScope.RefreshChildrens();
            }
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
