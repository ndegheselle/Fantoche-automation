using System;
using System.Threading.Tasks;
using Automation.App.Features.Scoped.Scopes;
using Automation.App.Features.Scoped.Tasks;
using Automation.App.Features.Scoped.Workflows;
using Automation.App.Services;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Scoped;

/// <summary>
/// Base view model wrapping a <see cref="ScopedElement"/> displayed in the workflows page.
/// </summary>
internal abstract partial class ScopedVm : ObservableObject
{
    protected readonly IScopedService _scopedService;
    public ScopedElement Element { get; }

    public ScopedMetadata Metadata => Element.Metadata;

    protected ScopedVm(ScopedElement element)
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
        // Edit a copy so the changes are discarded if the dialog is cancelled.
        var editVm = new Scoped.Components.MetadataEditVm(Metadata.Clone());
        ServiceProvider.Dialogs
            .CreateDialog(editVm)
            .WithSuccessCallback(() => ApplyEditAsync(editVm))
            .Dismissible()
            .WithMaxWidth(480)
            .Show();
    }

    private async Task ApplyEditAsync(Scoped.Components.MetadataEditVm editVm)
    {
        Element.Metadata = editVm.Build();
        OnPropertyChanged(nameof(Metadata));
        await _scopedService.EditAsync(Element);
    }

    /// <summary>
    /// Wrap [element] into its dedicated view model.
    /// </summary>
    public static ScopedVm From(ScopedElement element) => element switch
    {
        Scope scope => new ScopeVm(scope),
        AutomationWorkflow workflow => new WorkflowVm(workflow),
        AutomationTask task => new TaskVm(task),
        _ => throw new NotSupportedException($"No view model for scoped element '{element.GetType().Name}'.")
    };
}
