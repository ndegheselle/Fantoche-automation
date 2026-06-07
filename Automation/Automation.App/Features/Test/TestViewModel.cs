using Automation.App.Services.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Test
{
    internal partial class TestViewModel : ObservableObject, INavigable
    {
        public string Tata => "fafafa";

        private readonly ToastDisplay _toast;
        private readonly NavigationManager _navigation;

        public TestViewModel(ToastDisplay toast, NavigationManager navigation)
        {
            _toast = toast;
            _navigation = navigation;
        }

        [RelayCommand]
        public void TestToast()
        {
            _toast.Success("taof", "falmflam");
        }

        [RelayCommand]
        public void OpenModal()
        {
            _navigation.NavigateModal(new TestModalViewModel(_navigation));
        }
    }
}
