using System.Collections.Generic;
using System.Windows.Input;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Automation.App.Components.Inputs;

/// <summary>
/// Lets the user pick an existing <see cref="Scope"/> from a dropdown or create a new one
/// inline. Selection logic is exposed through <see cref="SelectedScope"/>; creation is
/// delegated to the host through <see cref="CreateScopeCommand"/> (invoked with the new
/// scope name as a string) so the control stays free of any service dependency.
/// </summary>
public partial class ScopeSelector : UserControl
{
    public static readonly StyledProperty<IEnumerable<Scope>?> ScopesProperty =
        AvaloniaProperty.Register<ScopeSelector, IEnumerable<Scope>?>(nameof(Scopes));

    public static readonly StyledProperty<Scope?> SelectedScopeProperty =
        AvaloniaProperty.Register<ScopeSelector, Scope?>(
            nameof(SelectedScope), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> NewScopeNameProperty =
        AvaloniaProperty.Register<ScopeSelector, string?>(nameof(NewScopeName));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<ScopeSelector, string?>(
            nameof(PlaceholderText), defaultValue: "Select a scope");

    /// <summary>
    /// Command invoked with the new scope name (a string) when the user creates a scope.
    /// The host is expected to persist it, add it to <see cref="Scopes"/> and optionally
    /// set it as <see cref="SelectedScope"/>.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CreateScopeCommandProperty =
        AvaloniaProperty.Register<ScopeSelector, ICommand?>(nameof(CreateScopeCommand));

    public ScopeSelector()
    {
        InitializeComponent();
        AddScopeCommand = new RelayCommand(_ => CreateScope());
    }

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

    public string? NewScopeName
    {
        get => GetValue(NewScopeNameProperty);
        set => SetValue(NewScopeNameProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public ICommand? CreateScopeCommand
    {
        get => GetValue(CreateScopeCommandProperty);
        set => SetValue(CreateScopeCommandProperty, value);
    }

    /// <summary>Internal command bound to the inline "add" button.</summary>
    public ICommand AddScopeCommand { get; }

    private void CreateScope()
    {
        var name = NewScopeName?.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        if (CreateScopeCommand?.CanExecute(name) == true)
            CreateScopeCommand.Execute(name);

        NewScopeName = string.Empty;
    }
}
