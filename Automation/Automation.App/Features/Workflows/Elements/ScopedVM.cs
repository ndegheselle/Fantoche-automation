using System;
using Automation.App.Services;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Elements;

/// <summary>
/// Base view model wrapping a <see cref="ScopedElement"/> displayed in the workflows page.
/// </summary>
internal abstract class ScopedVM : ObservableObject
{
    protected readonly IScopedService _scopedService;
    public ScopedElement Element { get; }

    public ScopedMetadata Metadata => Element.Metadata;

    protected ScopedVM(ScopedElement element)
    {
        _scopedService = ServiceProvider.Scoped;
        Element = element;
    }

    [RelayCommand]
    public void Remove()
    {

    }

    [RelayCommand]
    public void Edit()
    {

    }

    /// <summary>
    /// Wrap [element] into its dedicated view model.
    /// </summary>
    public static ScopedVM From(ScopedElement element) => element switch
    {
        Scope scope => new ScopeVM(scope),
        AutomationWorkflow workflow => new WorkflowVM(workflow),
        AutomationTask task => new TaskVM(task),
        _ => throw new NotSupportedException($"No view model for scoped element '{element.GetType().Name}'.")
    };
}
