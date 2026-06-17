using System;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace Automation.App.Components.Inputs;

public partial class DebouncedSearch : TextBox
{
    public static readonly DependencyProperty SearchProperty =
        DependencyProperty.Register(
            nameof(Search), typeof(string), typeof(DebouncedSearch),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private readonly DispatcherTimer _timer;

    public DebouncedSearch()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Search = Text;
            SearchChanged?.Invoke(this, new RoutedEventArgs());
        };

        TextChanged += (_, _) =>
        {
            _timer.Stop();
            _timer.Start();
        };
    }

    public string? Search
    {
        get => (string?)GetValue(SearchProperty);
        set => SetValue(SearchProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? SearchChanged;
}
