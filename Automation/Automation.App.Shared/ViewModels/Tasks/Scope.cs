using Automation.Shared.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public interface IViewModelNamed
    {
        Guid Id { get; }
        string Name { get; }
    }

    [Flags]
    public enum EnumScopedType
    {
        Scope,
        Workflow,
        Task
    }

    public class ScopedElement : IViewModelNamed, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        #region UI specific
        public Scope? Parent { get; set; }
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

        #endregion
    }

    public class Scope : ScopedElement
    {
        public Guid? ParentId { get; set; }
        public Dictionary<string, string> Context { get; private set; } = new Dictionary<string, string>();
        public List<ScopedElement> Childrens { get; set; } = new List<ScopedElement>();

        public Scope()
        {
            Type = EnumScopedType.Scope;
        }
    }
}
