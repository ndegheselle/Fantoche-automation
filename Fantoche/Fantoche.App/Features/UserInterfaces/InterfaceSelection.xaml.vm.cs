using Fantoche.App.Features.Packages.Controls;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// The interface the parameters of a task or a workflow are edited with (see
    /// <see cref="BaseAutomationTask.Interface"/>) : one picked from a package of interfaces, or the
    /// form generated from its parameters.
    /// </summary>
    public partial class InterfaceSelectionViewModel : ObservableObject
    {
        private readonly BaseAutomationTask _element;
        private readonly Action _changed;

        public PackageClassTarget? Target => _element.Interface;

        public bool HasTarget => Target != null;

        /// <param name="changed">Called once the element holds another interface, to be saved.</param>
        public InterfaceSelectionViewModel(BaseAutomationTask element, Action changed)
        {
            _element = element;
            _changed = changed;
        }

        [RelayCommand]
        private async Task Select()
        {
            PackageClassTarget? target = await PackageSelectionViewModel.ShowInterfacesAsync();
            if (target == null)
                return;
            SetTarget(target);
        }

        [RelayCommand]
        private void Remove() => SetTarget(null);

        private void SetTarget(PackageClassTarget? target)
        {
            _element.Interface = target;
            OnPropertyChanged(nameof(Target));
            OnPropertyChanged(nameof(HasTarget));
            _changed();
        }
    }
}
