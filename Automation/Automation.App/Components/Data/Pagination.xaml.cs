using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components.Data;

public partial class Pagination : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty TotalProperty =
        DependencyProperty.Register(nameof(Total), typeof(int), typeof(Pagination),
            new FrameworkPropertyMetadata(0, OnDependencyChanged));

    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(Pagination),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDependencyChanged));

    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(Pagination),
            new FrameworkPropertyMetadata(50, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDependencyChanged));

    public IReadOnlyList<int> PageSizes { get; } = [10, 25, 50, 100, 200];

    public Pagination()
    {
        InitializeComponent();
    }

    public int Total
    {
        get => (int)GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int PageSize
    {
        get => (int)GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public string RangeText
    {
        get
        {
            var from = (CurrentPage - 1) * PageSize + 1;
            var to = Total <= 0 ? CurrentPage * PageSize : Math.Min(CurrentPage * PageSize, Total);
            return Total <= 0 ? $"{from}-{to}" : $"{from}-{to} of {Total}";
        }
    }

    private static void OnDependencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((Pagination)d).Refresh();

    private void OnPrevious(object sender, RoutedEventArgs e)
    {
        if (HasPrevious) CurrentPage--;
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (HasNext) CurrentPage++;
    }

    private void Refresh()
    {
        // Clamp page size changes back to a valid page.
        if (CurrentPage > TotalPages)
        {
            CurrentPage = TotalPages; // re-enters Refresh via the changed callback
            return;
        }

        // Dependency properties don't notify the plain CLR computed properties bound in XAML,
        // so raise change notifications for them here.
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(RangeText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
