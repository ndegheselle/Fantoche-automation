using System.Collections.ObjectModel;
using System.Linq;
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
    
    public ObservableCollection<INavigable> Overlays { get; } = [];
    [ObservableProperty]
    private bool _hasOverlays;

    public NavigationManager()
    {
        Overlays.CollectionChanged += (_, _) => HasOverlays = Overlays.Count > 0;
    }


    public void Navigate(INavigable page)
    {
        if (page == CurrentPage)
            return;

        if (CurrentPage != null)
            CurrentPage.OnHide();

        CurrentPage = page;
        CurrentPage.OnShow();
    }
    
    public void Overlay(INavigable page)
    {
        Overlays.Add(page);
        page.OnShow();
    }
    
    /// <summary>
    /// Close a specific overlay [page].
    /// </summary>
    public void Close(INavigable page)
    {
        int overlayIndex = Overlays.IndexOf(page);
        if (overlayIndex != -1)
        {
            Overlays.RemoveAt(overlayIndex);
            page.OnHide();
        }
    }

    /// <summary>
    /// Close the last overlay.
    /// </summary>
    public void Close()
    {
        if (Overlays.Count <= 0)
            return;
        
        INavigable page = Overlays.Last();
        Overlays.RemoveAt(Overlays.Count - 1);
        page.OnHide();
    }
}
