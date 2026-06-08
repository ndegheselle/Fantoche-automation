using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Packages;

internal partial class PackageEditVM(NavigationManager navigation) : ObservableObject, INavigable
{
    [RelayCommand]
    public void Close()
    {
        navigation.Close(this);
    }
}