using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

    private readonly ObservableCollection<INavigable> _modalStack = new();
    public ObservableCollection<INavigable> ModalStack => _modalStack;

    [ObservableProperty]
    private bool _hasModals;

    public NavigationManager()
    {
        _modalStack.CollectionChanged += (_, _) => HasModals = _modalStack.Count > 0;
    }

    public void Navigate(INavigable page)
    {
        if (page == CurrentPage)
            return;

        CurrentPage?.OnHide();
        CurrentPage = page;
        CurrentPage.OnShow();
    }

    public void NavigateModal(INavigable page)
    {
        _modalStack.Add(page);
        page.OnShow();
    }

    [RelayCommand]
    public void CloseModal()
    {
        if (_modalStack.Count == 0)
            return;

        var top = _modalStack[^1];
        top.OnHide();
        _modalStack.RemoveAt(_modalStack.Count - 1);
    }
}
