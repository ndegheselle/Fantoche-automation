using Automation.Base.ViewModels;
using Automation.Supervisor.Repositories;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.App.ViewModels
{
    public class SideMenuViewModel : INotifyPropertyChanged
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

                // If the selected element is a scope and its childrens are not loaded, we load them
                // FIXME : handle empty scope
                if (_selectedElement != null &&
                    _selectedElement is Scope scope &&
                    scope.Childrens.Count == 0)
                {
                    ScopeRepository scopeRepository = new ScopeRepository();
                    Scope? fullScope = scopeRepository.GetScoped(_selectedElement.Id) as Scope;

                    foreach (ScopedElement child in fullScope.Childrens)
                        scope.AddChild(child);
                }

                OnPropertyChanged();
            }
        }

        public SideMenuViewModel()
        {
            ScopeRepository scopeRepository = new ScopeRepository();
            RootScope = scopeRepository.GetRootScope();
        }
    }
}
