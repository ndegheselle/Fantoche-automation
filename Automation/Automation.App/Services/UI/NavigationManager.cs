using System;
using System.Collections.Generic;
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
    
    private Dictionary<INavigable, Action> _onHiddenCallbacks = [];
    public ObservableCollection<INavigable> Overlays { get; } = [];
    [ObservableProperty]
    private bool _hasOverlays;

    public NavigationManager()
    {
        Overlays.CollectionChanged += (_, _) => HasOverlays = Overlays.Count > 0;
    }

    public void Navigate(INavigable page)
    {
        // Close all overlays on navigation
        for (int i = Overlays.Count - 1; i >= 0; i--)
        {
            INavigable overlay = Overlays[i];
            Overlays.RemoveAt(i);
            overlay.OnHide();
        }
        
        if (page == CurrentPage)
            return;

        CurrentPage?.OnHide();

        CurrentPage = page;
        CurrentPage.OnShow();
    }
    
    public void Overlay(INavigable page, Action? onHidden = null)
    {
        Overlays.Add(page);
        if (onHidden != null)
            _onHiddenCallbacks[page] = onHidden;
        page.OnShow();
    }
    
    /// <summary>
    /// Close a specific overlay [page].
    /// </summary>
    public void Close(INavigable page)
    {
        int overlayIndex = Overlays.IndexOf(page);
        if (overlayIndex == -1) return;

        Overlays.RemoveAt(overlayIndex);
        page.OnHide();
        
        if (_onHiddenCallbacks.TryGetValue(page, out Action? callback))
        {
            callback?.Invoke();
            _onHiddenCallbacks.Remove(page);
        }
    }

    /// <summary>
    /// Close the last overlay.
    /// </summary>
    public void Close()
    {
        if (Overlays.Count <= 0)
            return;
        Close(Overlays.Last());
    }
}
