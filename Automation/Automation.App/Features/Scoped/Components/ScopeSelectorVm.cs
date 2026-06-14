using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Automation.App.Base;
using Automation.App.Services;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Scoped.Components;

/// <summary>
/// A node in the scope tree displayed by <see cref="ScopeSelectorDialog"/>.
/// Supports inline creation of a child scope.
/// </summary>
internal partial class ScopeTreeNode : ObservableObject
{
    private readonly IScopedService _scopedService;

    public Scope Scope { get; }
    public string Name => Scope.Metadata.Name;
    public ObservableCollection<ScopeTreeNode> Children { get; } = new();

    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private bool _isCreatingChild;
    [ObservableProperty] private string _newChildName = "";

    public ScopeTreeNode(Scope scope, IScopedService scopedService)
    {
        Scope = scope;
        _scopedService = scopedService;
    }

    public async Task LoadChildrenAsync()
    {
        var elements = await _scopedService.GetChildrens(Scope.Id);
        Children.Clear();
        foreach (var child in elements.OfType<Scope>())
        {
            var node = new ScopeTreeNode(child, _scopedService);
            await node.LoadChildrenAsync();
            Children.Add(node);
        }
    }

    [RelayCommand]
    private void BeginAddChild()
    {
        NewChildName = "";
        IsCreatingChild = true;
    }

    [RelayCommand]
    private async Task ConfirmAddChild()
    {
        string name = NewChildName.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        var newScope = new Scope(name, Scope.Id);
        var created = (Scope)await _scopedService.CreateAsync(newScope);
        Children.Add(new ScopeTreeNode(created, _scopedService));

        NewChildName = "";
        IsCreatingChild = false;
    }

    [RelayCommand]
    private void CancelAddChild()
    {
        NewChildName = "";
        IsCreatingChild = false;
    }
}

/// <summary>
/// View model for <see cref="ScopeSelectorDialog"/>. Loads the scope hierarchy and lets
/// the user pick an existing scope or create a new one.
/// </summary>
internal partial class ScopeSelectorVm : ViewModelBase
{
    private readonly IScopedService _scopedService;

    public ObservableCollection<ScopeTreeNode> RootNodes { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private ScopeTreeNode? _selectedNode;

    /// <summary>The scope chosen by the user; set after <see cref="ConfirmCommand"/>.</summary>
    public Scope? SelectedScope => SelectedNode?.Scope;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isCreatingRootScope;
    [ObservableProperty] private string _newRootScopeName = "";

    public ScopeSelectorVm(IScopedService scopedService)
    {
        _scopedService = scopedService;
        if (scopedService != null)
            _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var elements = await _scopedService.GetChildrens(Scope.ROOT_SCOPE_ID);
            RootNodes.Clear();
            foreach (var child in elements.OfType<Scope>())
            {
                var node = new ScopeTreeNode(child, _scopedService);
                await node.LoadChildrenAsync();
                RootNodes.Add(node);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void BeginAddRootScope()
    {
        NewRootScopeName = "";
        IsCreatingRootScope = true;
    }

    [RelayCommand]
    private async Task ConfirmAddRootScope()
    {
        string name = NewRootScopeName.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        var newScope = new Scope(name, Scope.ROOT_SCOPE_ID);
        var created = (Scope)await _scopedService.CreateAsync(newScope);
        RootNodes.Add(new ScopeTreeNode(created, _scopedService));

        NewRootScopeName = "";
        IsCreatingRootScope = false;
    }

    [RelayCommand]
    private void CancelAddRootScope()
    {
        NewRootScopeName = "";
        IsCreatingRootScope = false;
    }

    private bool CanConfirm() => SelectedNode != null;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        ServiceProvider.Dialogs.Close(this, new CloseDialogOptions { Success = true });
    }

    [RelayCommand]
    private void Cancel() => ServiceProvider.Dialogs.Close(this);
}

internal class ScopeSelectorVmDesign : ScopeSelectorVm
{
    public ScopeSelectorVmDesign() : base(null!)
    {
        var ingestion = new ScopeTreeNode(
            new Scope { Metadata = new ScopedMetadata("Ingestion", EnumScopedType.Scope) }, null!);
        ingestion.Children.Add(new ScopeTreeNode(
            new Scope { Metadata = new ScopedMetadata("ETL", EnumScopedType.Scope) }, null!));

        RootNodes.Add(ingestion);
        RootNodes.Add(new ScopeTreeNode(
            new Scope { Metadata = new ScopedMetadata("Processing", EnumScopedType.Scope) }, null!));
    }
}
