using System.Windows;
using System.Windows.Controls;
using Automation.App.Features.Scoped.Components;
using Automation.App.Services;
using Automation.App.Services.UI;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Components.Inputs;

/// <summary>
/// A folder-input style control that displays the currently selected <see cref="Scope"/> and
/// lets the user pick another one through the <see cref="ScopeSelectorDialog"/>.
/// </summary>
public partial class ScopeSelectorInput : UserControl
{
    /// <summary>
    /// The currently selected scope. <c>null</c> is rendered as the root scope.
    /// </summary>
    public static readonly DependencyProperty SelectedScopeProperty =
        DependencyProperty.Register(
            nameof(SelectedScope), typeof(Scope), typeof(ScopeSelectorInput),
            new FrameworkPropertyMetadata(default(Scope), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Scope? SelectedScope
    {
        get => (Scope?)GetValue(SelectedScopeProperty);
        set => SetValue(SelectedScopeProperty, value);
    }

    public ScopeSelectorInput()
    {
        InitializeComponent();
    }

    private void OnChangeClick(object sender, RoutedEventArgs e)
    {
        var scopeVm = new ScopeSelectorVm(ServiceProvider.Scoped);
        ServiceProvider.Dialogs
            .CreateDialog(scopeVm)
            .WithSuccessCallback(() => SelectedScope = scopeVm.SelectedScope)
            .WithMaxWidth(520)
            .Show();
    }
}
