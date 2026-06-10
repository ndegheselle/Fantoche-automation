using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows;

internal partial class WorkflowsPageVM : ObservableObject, INavigable
{
    private readonly IScopedService _scopedService;
    private CancellationTokenSource? _cts;

    public WorkflowsPageVM(IScopedService scopedService)
    {
        _scopedService = scopedService;
    }

    /// <summary>
    /// Top-level scoped elements (scopes, workflows and tasks) shown in the tree.
    /// </summary>
    public ObservableCollection<ScopedElement> Items { get; } = new();

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _isLoading;

    /// <summary>
    /// Element currently selected in the tree.
    /// </summary>
    [ObservableProperty] private ScopedElement? _selected;

    public void OnShow() => _ = RefreshAsync();

    public void OnHide() => _cts?.Cancel();

    partial void OnSearchTextChanged(string value) => _ = RefreshAsync();

    /// <summary>
    /// Create a new element of the given [type], inside the selected scope if any.
    /// </summary>
    [RelayCommand]
    private async Task AddAsync(EnumScopedType type)
    {
        ScopedElement element = type switch
        {
            EnumScopedType.Workflow => new AutomationWorkflow(),
            EnumScopedType.Task => new AutomationTask(),
            _ => new Scope()
        };
        element.Metadata.Name = $"New {type.ToString().ToLowerInvariant()}";

        Scope? parent = Selected as Scope ?? Selected?.Parent;
        await _scopedService.CreateAsync(element, parent);

        // Childrens of a scope are observable, only root elements need to be added manually
        if (parent == null)
            Items.Add(element);

        Selected = element;
    }

    private async Task RefreshAsync()
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;

        try
        {
            IsLoading = true;
            var tree = await _scopedService.GetTreeAsync(SearchText?.Trim() ?? string.Empty);

            if (token.IsCancellationRequested)
                return;

            Items.Clear();
            foreach (var element in tree)
                Items.Add(element);
        }
        finally
        {
            if (ReferenceEquals(_cts, cts))
                IsLoading = false;
        }
    }
}

/// <summary>
/// Design class for [WorkflowsPageVM]
/// </summary>
internal class WorkflowsPageVMDesign : WorkflowsPageVM
{
    public WorkflowsPageVMDesign() : base(null!)
    {
        var scope = new Scope { Metadata = new ScopedMetadata("Ingestion", EnumScopedType.Scope) };
        scope.AddChild(new AutomationWorkflow { Metadata = new ScopedMetadata("Daily import", EnumScopedType.Workflow) });
        scope.AddChild(new AutomationTask { Metadata = new ScopedMetadata("Fetch files", EnumScopedType.Task) });

        Items.Add(scope);
        Items.Add(new AutomationWorkflow { Metadata = new ScopedMetadata("Weekly report", EnumScopedType.Workflow) });
        Items.Add(new AutomationTask { Metadata = new ScopedMetadata("Cleanup temp", EnumScopedType.Task) });

        IsLoading = false;
    }
}
