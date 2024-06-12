using Automation.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.App.Contexts
{
    public class SideMenuContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Scope RootScope { get; set; }

        private ScopedElement? _selectedElement;

        public ScopedElement? SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (_selectedElement == value)
                    return;

                _selectedElement = value;
                OnPropertyChanged();
            }
        }

        public SideMenuContext()
        {
            RootScope = new Scope();
            RootScope.Childrens
                .Add(
                    new Scope()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Scope 1",
                        Childrens =
                            new ObservableCollection<ScopedElement>()
                                {
                                    new WorkflowScope() { Id = Guid.NewGuid(), Name = "Workflow test" },
                                    new TaskScope()
                                    {
                                        Id = Guid.NewGuid(),
                                        Name = "Wait all",
                                    },
                                    new TaskScope()
                                    {
                                        Id = Guid.NewGuid(),
                                        Name = "Delay",
                                    },
                                }
                    });
        }
    }
}
