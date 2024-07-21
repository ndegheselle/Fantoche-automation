using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
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
        public string Name { get; set; }

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

    public class TaskScopedItem : ScopedItem
    {
        public TaskNode Task { get; set; }
        
        public TaskScopedItem(TaskNode task)
        {
            Task = task;
            Name = task.Name;
            Type = EnumScopedType.Task;
        }
    }

    public class WorkflowScopedItem : ScopedItem
    {
        public WorkflowNode Workflow { get; set; }

        public WorkflowScopedItem(WorkflowNode workflow)
        {
            Workflow = workflow;
            Name = workflow.Name;
            Type = EnumScopedType.Workflow;
        }
    }

    public class ScopeItem : ScopedItem
    {
        public Scope Scope { get; set; }
        public ObservableCollection<ScopedItem> Childrens { get; set; } = new ObservableCollection<ScopedItem>();
        public ListCollectionView SortedChildrens { get; set; }

        public ScopeItem(Scope scope)
        {
            Scope = scope;
            Name = scope.Name;
            Type = EnumScopedType.Scope;

            foreach (var subScope in scope.SubScope)
            {
                Childrens.Add(new ScopeItem(subScope));
            }
            foreach (var task in scope.Childrens)
            {
                if (task is TaskNode taskNode)
                    Childrens.Add(new TaskScopedItem(taskNode));
                else if (task is WorkflowNode workflow)
                    Childrens.Add(new WorkflowScopedItem(workflow));
            }

            Type = EnumScopedType.Scope;
            SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Type), ListSortDirection.Ascending));
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
        }
    }
}
