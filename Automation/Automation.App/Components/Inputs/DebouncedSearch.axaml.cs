using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Automation.App.Components.Inputs;

public partial class DebouncedSearch : UserControl
{
    public static readonly StyledProperty<string?> SearchProperty =
        AvaloniaProperty.Register<DebouncedSearch, string?>(
            nameof(Search), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly RoutedEvent<RoutedEventArgs> SearchChangedEvent =
        RoutedEvent.Register<DebouncedSearch, RoutedEventArgs>(
            nameof(SearchChanged), RoutingStrategies.Bubble);

    private readonly DispatcherTimer _timer;

    public DebouncedSearch()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Search = SearchInput.Text;
            RaiseEvent(new RoutedEventArgs(SearchChangedEvent));
        };

        SearchInput.TextChanged += (_, _) =>
        {
            _timer.Stop();
            _timer.Start();
        };
    }

    public string? Search
    {
        get => GetValue(SearchProperty);
        set => SetValue(SearchProperty, value);
    }

    public event EventHandler<RoutedEventArgs> SearchChanged
    {
        add => AddHandler(SearchChangedEvent, value);
        remove => RemoveHandler(SearchChangedEvent, value);
    }
}