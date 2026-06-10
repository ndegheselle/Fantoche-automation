using System.Collections.Generic;
using System.Linq;
using Automation.App.Assets.Fonts;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        IconList.SelectionChanged += OnSelectionChanged;
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
        IconList.ItemsSource = string.IsNullOrEmpty(search)
            ? LucideIconCatalog.All
            : LucideIconCatalog.All
                .Where(icon => icon.Name.Contains(search, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IconList.SelectedItem is LucideIconItem item)
        {
            SelectedIcon = item.Glyph;
            // Close the flyout once an icon has been chosen.
            TriggerButton.Flyout?.Hide();
        }
    }

    private void SyncSelectionFromValue()
    {
        IconList.SelectedItem = SelectedIcon is null
            ? null
            : ((IEnumerable<LucideIconItem>)IconList.ItemsSource!)
                .FirstOrDefault(icon => icon.Glyph == SelectedIcon);
    }
}
