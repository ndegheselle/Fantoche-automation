using Avalonia.Controls;
using Avalonia.Input;

namespace Automation.App.Features.Workflows.Components;

/// <summary>
/// Header of a <see cref="Automation.Shared.Data.Scoped.ScopedElement"/> page, shared between scope, workflow and task pages.
/// </summary>
public partial class ElementHeader : UserControl
{
    public ElementHeader()
    {
        InitializeComponent();
    }
}
