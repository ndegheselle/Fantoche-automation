using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App;

internal interface INavigable
{
    public void OnShow() { }
    public void OnHide() { }
}

internal partial class NavigationManager : ObservableObject
{
    [ObservableProperty]
    private INavigable? _currentPage;

    public void Navigate(INavigable page)
    {
        if (page == CurrentPage)
            return;

        if (CurrentPage != null)
            CurrentPage.OnHide();

        CurrentPage = page;
        CurrentPage.OnShow();
    }
}
