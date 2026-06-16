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
/// Supports explorer-style inline renaming and the creation of new (not yet persisted) scopes.
/// </summary>
internal partial class ScopeTreeNode : ObservableObject
{
    private readonly IScopedService _scopedService;
    private readonly ScopeSelectorVm _owner;

    /// <summary>The underlying scope. For a not-yet-persisted node its <see cref="Scope.Id"/> is empty.</summary>
    public Scope Scope { get; }

    /// <summary>Parent node, or <c>null</c> when the node lives at the root level.</summary>
    public ScopeTreeNode? Parent { get; }

    public ObservableCollection<ScopeTreeNode> Children { get; } = new();

    /// <summary>Collection this node belongs to (its parent's children, or the root collection).</summary>
    private ObservableCollection<ScopeTreeNode> Siblings => Parent?.Children ?? _owner.RootNodes;

    /// <summary>Displayed name. Kept in sync with <see cref="ScopedMetadata.Name"/> on commit.</summary>
    [ObservableProperty] private string _name;

    [ObservableProperty] private bool _isExpanded = true;

    /// <summary>When <c>true</c> the inline rename box is shown instead of the label.</summary>
    [ObservableProperty] private bool _isEditing;

    /// <summary>Buffer edited by the rename box; only written back on a successful commit.</summary>
    [ObservableProperty] private string _editingName = "";

    /// <summary><c>true</c> until the scope has been persisted once; cancelling such a node removes it.</summary>
    private bool _isNew;

    public ScopeTreeNode(
        Scope scope,
        ScopeTreeNode? parent,
        ScopeSelectorVm owner,
        IScopedService scopedService,
        bool isNew = false)
    {
        Scope = scope;
        Parent = parent;
        _owner = owner;
        _scopedService = scopedService;
        _isNew = isNew;
        _name = scope.Metadata.Name;
    }

    public async Task LoadChildrenAsync()
    {
        var elements = await _scopedService.GetChildrens(Scope.Id);
        Children.Clear();
        foreach (var child in elements.OfType<Scope>())
        {
            var node = new ScopeTreeNode(child, this, _owner, _scopedService);
            await node.LoadChildrenAsync();
            Children.Add(node);
        }
    }

    /// <summary>Enters inline edit mode, pre-filling the box with the current name.</summary>
    public void BeginRename()
    {
        EditingName = Name;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task CommitEdit()
    {
        if (!IsEditing)
            return;

        string name = EditingName.Trim();
        // An empty name is treated as "abort": cancel keeps existing nodes and drops brand new ones.
        if (string.IsNullOrEmpty(name))
        {
            CancelEdit();
            return;
        }

        Guid parentId = Parent?.Scope.Id ?? Scope.ROOT_SCOPE_ID;
        Guid? excludeId = _isNew ? null : Scope.Id;
        bool isUnique = await _scopedService.IsNameUniqueAsync(parentId, name, excludeId);
        if (!isUnique)
        {
            // Keep editing so the user can correct the name.
            ServiceProvider.Toasts.Value.Error(
                "Name already used",
                $"A scope named \"{name}\" already exists here.");
            return;
        }

        Scope.Metadata.Name = name;
        if (_isNew)
        {
            await _scopedService.CreateAsync(Scope);
            _isNew = false;
        }
        else
        {
            await _scopedService.EditAsync(Scope);
        }

        Name = name;
        IsEditing = false;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingName = "";

        if (_isNew)
        {
            // The node was never persisted: remove it from the tree entirely.
            Siblings.Remove(this);
            if (_owner.SelectedNode == this)
                _owner.SelectedNode = null;
        }
    }
}

/// <summary>
/// View model for <see cref="ScopeSelectorDialog"/>. Loads the scope hierarchy and lets the user
/// pick an existing scope, create new ones inline, or rename them.
/// </summary>
internal partial class ScopeSelectorVm : ViewModelBase
{
    private readonly IScopedService _scopedService;

    public ObservableCollection<ScopeTreeNode> RootNodes { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameScopeCommand))]
    private ScopeTreeNode? _selectedNode;

    /// <summary>The scope chosen by the user; set after <see cref="ConfirmCommand"/>.</summary>
    public Scope? SelectedScope => SelectedNode?.Scope;

    [ObservableProperty] private bool _isLoading;

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
                var node = new ScopeTreeNode(child, null, this, _scopedService);
                await node.LoadChildrenAsync();
                RootNodes.Add(node);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Adds a new scope under the selected node, or at the root level when nothing is selected,
    /// then drops straight into inline editing so the user can name it.
    /// </summary>
    [RelayCommand]
    private void AddScope()
    {
        var parent = SelectedNode;
        Guid parentId = parent?.Scope.Id ?? Scope.ROOT_SCOPE_ID;

        var scope = new Scope("New scope", parentId);
        var node = new ScopeTreeNode(scope, parent, this, _scopedService, isNew: true);

        if (parent != null)
        {
            parent.Children.Add(node);
            parent.IsExpanded = true;
        }
        else
        {
            RootNodes.Add(node);
        }

        SelectedNode = node;
        node.BeginRename();
    }

    private bool HasSelection() => SelectedNode != null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RenameScope() => SelectedNode?.BeginRename();

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
            new Scope { Metadata = new ScopedMetadata("Ingestion", EnumScopedType.Scope) },
            null, this, null!);
        ingestion.Children.Add(new ScopeTreeNode(
            new Scope { Metadata = new ScopedMetadata("ETL", EnumScopedType.Scope) },
            ingestion, this, null!));

        RootNodes.Add(ingestion);
        RootNodes.Add(new ScopeTreeNode(
            new Scope { Metadata = new ScopedMetadata("Processing", EnumScopedType.Scope) },
            null, this, null!));
    }
}
