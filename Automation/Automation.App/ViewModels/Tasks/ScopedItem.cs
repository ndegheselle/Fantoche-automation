using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace Automation.App.ViewModels.Tasks.BAK
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
        public Guid TargetId { get; set; }

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

        public ScopedItem(EnumScopedType type, Guid targetId, string name)
        {
            Type = type;
            Name = name;
            TargetId = targetId;
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
        public ScopedTaskItem(TaskNode task) : base(task is WorkflowNode ? EnumScopedType.Workflow : EnumScopedType.Task, task.Id, task.Name)
        {}
    }

    public class ScopeItem : ScopedItem
    {
        public ObservableCollection<ScopedItem> Childrens { get; set; } = new ObservableCollection<ScopedItem>();
        public ListCollectionView SortedChildrens { get; set; }

        public ScopeItem(Scope scope) : base(EnumScopedType.Scope, scope.Id, scope.Name)
        {
            RefreshChildrens(scope);

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
