using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Test;

internal partial class TestModalViewModel : ObservableObject, INavigable
{
    private readonly NavigationManager _navigation;
    public string Title { get; }

    public TestModalViewModel(NavigationManager navigation, string title = "Test Modal")
    {
        _navigation = navigation;
        Title = title;
    }

    [RelayCommand]
    private void Close() => _navigation.CloseModal();

    [RelayCommand]
    private void OpenNestedModal() =>
        _navigation.NavigateModal(new TestModalViewModel(_navigation, "Nested Modal"));
}
