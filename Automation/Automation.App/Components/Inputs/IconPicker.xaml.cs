using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Automation.App.Assets.Fonts;

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
    public static readonly DependencyProperty SelectedIconProperty =
        DependencyProperty.Register(
            nameof(SelectedIcon), typeof(string), typeof(IconPicker),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? SelectedIcon
    {
        get => (string?)GetValue(SelectedIconProperty);
        set => SetValue(SelectedIconProperty, value);
    }

    /// <summary>
    /// When <c>true</c>, the icon is displayed but the picker flyout can no longer be opened.
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(IconPicker),
            new FrameworkPropertyMetadata(false));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public IconPicker()
    {
        InitializeComponent();

        IconList.ItemsSource = LucideIconCatalog.All;
    }

    private void OnTriggerClick(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly)
            return;

        PickerPopup.IsOpen = !PickerPopup.IsOpen;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        string search = SearchBox.Text?.Trim() ?? string.Empty;
        IReadOnlyList<LucideIconItem> icons = string.IsNullOrEmpty(search)
            ? LucideIconCatalog.All
            : LucideIconCatalog.All
                .Where(icon => icon.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        IconList.ItemsSource = icons;
        EmptyPlaceholder.Visibility = icons.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnIconTapped(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LucideIconItem item })
        {
            SelectedIcon = item.Glyph;
            PickerPopup.IsOpen = false;
        }
    }
}
