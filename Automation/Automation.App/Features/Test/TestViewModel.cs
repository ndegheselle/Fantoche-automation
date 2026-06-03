using Automation.App.Services.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Test
{
    internal partial class TestViewModel : ObservableObject, INavigable
    {
        public string Tata => "fafafa";

        private readonly ToastDisplay _toast;
        public TestViewModel(ToastDisplay toast)
        {
            _toast = toast;
        }

        [RelayCommand]
        public void TestToast()
        {
            _toast.Success("taof", "falmflam");
        }
    }
}
