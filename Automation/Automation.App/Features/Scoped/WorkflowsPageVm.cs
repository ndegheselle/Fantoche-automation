using System;
using System.Threading.Tasks;
using Automation.App.Features.Scoped.Scopes;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Scoped;

internal partial class WorkflowsPageVm : ObservableObject, INavigable
{
    private readonly IScopedService _scopedService;

    public WorkflowsPageVm(IScopedService scopedService)
    {
        _scopedService = scopedService;
        _root = new ScopeVm(new() { Id = Scope.ROOT_SCOPE_ID, Metadata = new ScopedMetadata("Home", EnumScopedType.Scope) { IsReadOnly = true } });
    }

    #region Properties

    /// <summary>
    /// Virtual root scope, its childrens are the top-level elements returned by the service.
    /// </summary>
    [ObservableProperty]
    private ScopeVm _root;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _isLoading;

    /// <summary>
    /// Element currently selected in the list.
    /// </summary>
    [ObservableProperty] private ScopedVm? _selected;
    #endregion

    #region Events
    public void OnShow() => _ = Root.LoadChildren();

    partial void OnSearchTextChanged(string value)
    {
        // TODO : on search should display a list of result instead of the treeview
        throw new NotImplementedException();
    }
    #endregion

    /// <summary>
    /// Create a new element of the given [type] inside the current scope.
    /// </summary>
    [RelayCommand]
    private async Task AddAsync(EnumScopedType type)
    {
    }
}