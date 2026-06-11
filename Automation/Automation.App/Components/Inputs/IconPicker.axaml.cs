using System.Collections.Generic;
using System.Linq;
using Automation.App.Assets.Fonts;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Automation.App.Components.Inputs;

/// <summary>
/// Lets the user pick an icon from <see cref="LucideIconFont"/>. The selected value is the icon
/// glyph (the font character), ready to be rendered with the <c>LucideIcon</c> text style.
/// </summary>
public partial class IconPicker : UserControl
{
    /// <summary>
    /// The currently selected icon glyph, or <c>null</c> when none is selected.
    /// </summary>
    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<IconPicker, string?>(
            nameof(SelectedIcon), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public string? SelectedIcon
    {
        get => GetValue(SelectedIconProperty);
        set => SetValue(SelectedIconProperty, value);
    }

    public IconPicker()
    {
        InitializeComponent();
        
        IconList.ItemsSource = LucideIconCatalog.All;
        SearchBox.TextChanged += OnSearchChanged;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        SyncSelectionFromValue();
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        string search = SearchBox.Text?.Trim() ?? string.Empty;
        IReadOnlyList<LucideIconItem> icons = string.IsNullOrEmpty(search)
            ? LucideIconCatalog.All
            : LucideIconCatalog.All
                .Where(icon => icon.Name.Contains(search, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

        IconList.ItemsSource = icons;
        EmptyPlaceholder.IsVisible = icons.Count == 0;
    }

    private void OnIconTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: LucideIconItem item })
        {
            SelectedIcon = item.Glyph;
            TriggerButton.Flyout?.Hide();
        }
    }

    private void SyncSelectionFromValue()
    {
    }
}
