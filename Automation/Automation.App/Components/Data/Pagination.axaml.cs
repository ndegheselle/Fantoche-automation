using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Automation.App.Components.Data;

public partial class Pagination : UserControl
{
    public static readonly StyledProperty<int> TotalProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(Total));

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<Pagination, int>(
            nameof(CurrentPage), defaultValue: 1,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> PageSizeProperty =
        AvaloniaProperty.Register<Pagination, int>(
            nameof(PageSize), defaultValue: 100,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public IReadOnlyList<int> PageSizes { get; } = [10, 25, 50, 100, 200];

    static Pagination()
    {
        // Recompute derived display whenever any of these change.
        TotalProperty.Changed.AddClassHandler<Pagination>((s, _) => s.Refresh());
        CurrentPageProperty.Changed.AddClassHandler<Pagination>((s, _) => s.Refresh());
        PageSizeProperty.Changed.AddClassHandler<Pagination>((s, _) => s.Refresh());
    }

    public Pagination()
    {
        InitializeComponent();
    }

    public int Total
    {
        get => GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int PageSize
    {
        get => GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public string RangeText
    {
        get
        {
            if (Total == 0) return "0-0 of 0";
            var from = (CurrentPage - 1) * PageSize + 1;
            var to = Math.Min(CurrentPage * PageSize, Total);
            return $"{from}-{to} of {Total}";
        }
    }

    private void OnPrevious(object? sender, RoutedEventArgs e)
    {
        if (HasPrevious) CurrentPage--;
    }

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        if (HasNext) CurrentPage++;
    }

    private void Refresh()
    {
        // Clamp page size changes back to a valid page.
        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages; // re-enters Refresh via the changed handler
        }
    }
}