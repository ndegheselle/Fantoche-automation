using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.App.ViewModels.Scopes
{
    public enum EnumTaskType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedElement : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; } = Guid.NewGuid();

        public Scope Parent { get; set; }

        public string Name { get; set; }

        public EnumTaskType Type { get; set; }

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

        private void ExpandParent()
        {
            if (Parent == null)
                return;

            Parent.IsExpanded = true;
            Parent.ExpandParent();
        }
    }

    // Move to 
    public class Scope : ScopedElement
    {
        public ObservableCollection<ScopedElement> Childrens { get; set; } = [];
        public Scope() { Type = EnumTaskType.Scope; }

        public void AddChild(ScopedElement child)
        {
            child.Parent = this;
            Childrens.Add(child);
        }

        public bool IsExpanded { get; set; }
    }
}
