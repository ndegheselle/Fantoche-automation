using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Features.History;

/// <summary>
/// Reusable table that displays a paginated, auto-refreshing execution history. Its
/// <see cref="FrameworkElement.DataContext"/> is expected to be a <see cref="HistoryVmBase"/>; the
/// control drives that view model's <see cref="HistoryVmBase.Start"/> / <see cref="HistoryVmBase.Stop"/>
/// lifecycle from its own visual lifecycle so the page hosting it doesn't have to.
/// </summary>
public partial class HistoryTable : UserControl
{
    private HistoryVmBase? _active;
    private bool _isAttached;

    public HistoryTable()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isAttached = true;
        Activate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isAttached = false;
        Deactivate();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // The hosting ContentControl can swap the bound element while keeping this control attached
        // (e.g. selecting another task), so re-bind the lifecycle to the new view model.
        if (_active != null || DataContext is HistoryVmBase)
        {
            Deactivate();
            Activate();
        }
    }

    private void Activate()
    {
        if (DesignerProperties.GetIsInDesignMode(this) || !_isAttached)
            return;

        _active = DataContext as HistoryVmBase;
        _active?.Start();
    }

    private void Deactivate()
    {
        _active?.Stop();
        _active = null;
    }
}
