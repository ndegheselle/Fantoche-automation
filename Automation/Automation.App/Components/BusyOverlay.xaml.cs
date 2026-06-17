using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components;

/// <summary>
/// A simple overlay showing an indeterminate progress ring while <see cref="IsBusy"/> is true.
/// Replaces the ShadUI <c>BusyArea</c>; place it as the last child of a Grid/Panel to overlay siblings.
/// </summary>
public partial class BusyOverlay : UserControl
{
    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(BusyOverlay), new PropertyMetadata(false));

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public BusyOverlay()
    {
        InitializeComponent();
    }
}
