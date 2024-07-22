using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace Automation.App.ViewModels.Tasks
{
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public ScopeItem? Parent { get; set; }
        public string Name { get; protected set; }

        public EnumScopedType Type { get; set; }
        public bool IsExpanded { get; set; }

        private bool _isSelected;
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

    public class ScopedTaskItem : ScopedItem
    {
        public TaskNode TaskNode { get; set; }
        
        public ScopedTaskItem(TaskNode task)
        {
            TaskNode = task;
            Name = task.Name;
            Type = EnumScopedType.Task;
        }
    }

    public class ScopeItem : ScopedItem
    {
        public Scope ScopeNode { get; set; }
        public ObservableCollection<ScopedItem> Childrens { get; set; } = new ObservableCollection<ScopedItem>();
        public ListCollectionView SortedChildrens { get; set; }

        public ScopeItem(Scope scope)
        {
            ScopeNode = scope;
            Name = scope.Name;
            Type = EnumScopedType.Scope;

            RefreshChildrens(ScopeNode);

            SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Type), ListSortDirection.Ascending));
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
        }

        public void RefreshChildrens(Scope scope)
        {
            Childrens.Clear();
            foreach (var subScope in scope.SubScope)
            {
                Childrens.Add(new ScopeItem(subScope));
            }
            foreach (var task in scope.Childrens)
            {
                var taskScopedItem = new ScopedTaskItem(task)
                {
                    Type = (task is WorkflowNode) ? EnumScopedType.Workflow : EnumScopedType.Task
                };
                Childrens.Add(taskScopedItem);
            }
        }
    }
}
