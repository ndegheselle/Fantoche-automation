using Automation.App.ViewModels.Graph;
using Automation.Base;
using Automation.Supervisor.Repositories;
using System.Collections.ObjectModel;
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

        private NodeWrapper? _selectedElement;

        public NodeWrapper? SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (_selectedElement == value)
                    return;

                _selectedElement = value;

                if (_selectedElement != null &&
                    _selectedElement is ScopeWrapper scopeWrapper &&
                    scopeWrapper.Childrens == null)
                {
                    ScopeRepository scopeRepository = new ScopeRepository();
                    _selectedElement = new ScopeWrapper((Scope)scopeRepository.GetNode(_selectedElement.Node.Id));
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
