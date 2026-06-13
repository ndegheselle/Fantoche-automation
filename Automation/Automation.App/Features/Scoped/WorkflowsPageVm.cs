using System;
using System.Threading.Tasks;
using Automation.App.Features.Scoped.Scopes;
using Automation.App.Services;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

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
    private void Add(EnumScopedType type)
    {
        var parent = (Selected as ScopeVm) ?? Root;
        ScopedElement element = type switch
        {
            EnumScopedType.Scope => new Scope("New scope", parent.Scope.Id),
            EnumScopedType.Workflow => new AutomationWorkflow("New workflow", parent.Scope.Id),
            EnumScopedType.Task => new AutomationTask("New task", parent.Scope.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        var editVm = new Scoped.Components.MetadataEditVm(element);
        ServiceProvider.Dialogs
            .CreateDialog(editVm)
            .WithSuccessCallback(() => ApplyAddAsync(editVm, element, parent))
            .Dismissible()
            .WithMaxWidth(480)
            .Show();
    }

    private async Task ApplyAddAsync(Scoped.Components.MetadataEditVm editVm, ScopedElement element, ScopeVm parent)
    {
        element.Metadata = editVm.Build();
        await parent.AddChild(element);
    }
}