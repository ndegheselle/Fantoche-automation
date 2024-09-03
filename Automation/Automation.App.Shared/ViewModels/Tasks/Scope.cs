using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Data;

namespace Automation.App.Shared.ViewModels.Tasks
{
    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedElement : INamed, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        #region UI specific
        [JsonIgnore]
        public Scope? Parent { get; set; }
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

        #endregion
    }

    public class Scope : ScopedElement, IScope
    {
        public Guid? ParentId { get; set; }
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();

        private ObservableCollection<ScopedElement> _childrens = [];
        public ObservableCollection<ScopedElement> Childrens
        {
            get { return _childrens; }
            set
            {
                _childrens = value;
                SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
                SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Type), ListSortDirection.Ascending));
                SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));

                RefreshChildrens();
            }
        }
        [JsonIgnore]
        IList<INamed> IScope.Childrens => Childrens.Cast<INamed>().ToList();

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
        }

        public void AddChild(ScopedElement element)
        {
            element.Parent = this;
            Childrens.Add(element);
        }
    }
}
