using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Automation.App.Components.Inputs;

/// <summary>
/// Lets the user pick a <see cref="Scope"/> from a hierarchical tree or create a new one
/// inline. The flat <see cref="Scopes"/> list is turned into a tree by parent id. Selection
/// is exposed through <see cref="SelectedScope"/>; creation is delegated to the host through
/// <see cref="CreateScopeCommand"/> (invoked with the new scope name) so the control stays
/// free of any service dependency. The host is expected to create the scope under the
/// currently <see cref="SelectedScope"/>.
/// </summary>
public partial class ScopeSelector : UserControl
{
    public static readonly StyledProperty<IEnumerable<Scope>?> ScopesProperty =
        AvaloniaProperty.Register<ScopeSelector, IEnumerable<Scope>?>(nameof(Scopes));

    public static readonly StyledProperty<Scope?> SelectedScopeProperty =
        AvaloniaProperty.Register<ScopeSelector, Scope?>(
            nameof(SelectedScope), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ScopeNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<ScopeSelector, ScopeNode?>(nameof(SelectedNode));

    public static readonly StyledProperty<string?> NewScopeNameProperty =
        AvaloniaProperty.Register<ScopeSelector, string?>(nameof(NewScopeName));

    /// <summary>
    /// Command invoked with the new scope name (a string) when the user creates a scope.
    /// The host is expected to persist it (under <see cref="SelectedScope"/>), add it to
    /// <see cref="Scopes"/> and select it.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CreateScopeCommandProperty =
        AvaloniaProperty.Register<ScopeSelector, ICommand?>(nameof(CreateScopeCommand));

    private bool _syncing;
    private INotifyCollectionChanged? _scopesNotifier;

    public ScopeSelector()
    {
        InitializeComponent();
        AddScopeCommand = new RelayCommand(_ => CreateScope());
    }

    /// <summary>Tree built from <see cref="Scopes"/>, bound by the TreeView.</summary>
    public ObservableCollection<ScopeNode> Roots { get; } = new();

    public IEnumerable<Scope>? Scopes
    {
        get => GetValue(ScopesProperty);
        set => SetValue(ScopesProperty, value);
    }

    public Scope? SelectedScope
    {
        get => GetValue(SelectedScopeProperty);
        set => SetValue(SelectedScopeProperty, value);
    }

    public ScopeNode? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public string? NewScopeName
    {
        get => GetValue(NewScopeNameProperty);
        set => SetValue(NewScopeNameProperty, value);
    }

    public ICommand? CreateScopeCommand
    {
        get => GetValue(CreateScopeCommandProperty);
        set => SetValue(CreateScopeCommandProperty, value);
    }

    /// <summary>Internal command bound to the inline "add" button.</summary>
    public ICommand AddScopeCommand { get; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ScopesProperty)
        {
            if (_scopesNotifier != null)
                _scopesNotifier.CollectionChanged -= OnScopesChanged;

            _scopesNotifier = Scopes as INotifyCollectionChanged;
            if (_scopesNotifier != null)
                _scopesNotifier.CollectionChanged += OnScopesChanged;

            RebuildTree();
        }
        else if (change.Property == SelectedNodeProperty && !_syncing)
        {
            _syncing = true;
            SelectedScope = SelectedNode?.Scope;
            _syncing = false;
        }
        else if (change.Property == SelectedScopeProperty && !_syncing)
        {
            _syncing = true;
            SelectedNode = FindNode(Roots, SelectedScope);
            _syncing = false;
        }
    }

    private void OnScopesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildTree();

    private void RebuildTree()
    {
        // Avoid the transient SelectedItem reset (while the tree is repopulated) from
        // clearing SelectedScope.
        _syncing = true;
        Roots.Clear();

        if (Scopes != null)
        {
            var nodes = new Dictionary<Guid, ScopeNode>();
            foreach (var scope in Scopes)
                nodes[scope.Id] = new ScopeNode(scope);

            foreach (var node in nodes.Values)
            {
                if (node.Scope.ParentId is Guid parentId && nodes.TryGetValue(parentId, out var parent))
                    parent.Children.Add(node);
                else
                    Roots.Add(node);
            }
        }

        SelectedNode = FindNode(Roots, SelectedScope);
        _syncing = false;
    }

    private void CreateScope()
    {
        var name = NewScopeName?.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        if (CreateScopeCommand?.CanExecute(name) == true)
            CreateScopeCommand.Execute(name);

        NewScopeName = string.Empty;
    }

    private static ScopeNode? FindNode(IEnumerable<ScopeNode> nodes, Scope? scope)
    {
        if (scope == null)
            return null;

        foreach (var node in nodes)
        {
            if (node.Scope.Id == scope.Id)
                return node;

            var found = FindNode(node.Children, scope);
            if (found != null)
                return found;
        }

        return null;
    }
}

/// <summary>Tree node wrapping a <see cref="Scope"/> and its children.</summary>
public class ScopeNode
{
    public ScopeNode(Scope scope)
    {
        Scope = scope;
    }

    public Scope Scope { get; }

    public ObservableCollection<ScopeNode> Children { get; } = new();

    /// <summary>Expanded by default so the whole tree (and new scopes) stay visible.</summary>
    public bool IsExpanded { get; set; } = true;
}
