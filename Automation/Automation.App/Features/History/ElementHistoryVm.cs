using System;
using System.Threading.Tasks;
using Automation.App.Services;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;

namespace Automation.App.Features.History;

/// <summary>
/// History scoped to a single scoped element (task, workflow or scope). Used by the embedded
/// <see cref="HistoryTable"/> on the element pages.
/// </summary>
internal partial class ElementHistoryVm : HistoryVmBase
{
    private readonly IScopedService _scopedService;
    private readonly Guid _elementId;

    public ElementHistoryVm(Guid elementId)
        : this(elementId, ServiceProvider.Scoped, ServiceProvider.History.Value)
    {
    }

    public ElementHistoryVm(Guid elementId, IScopedService scopedService, IHistoryService historyService)
        : base(historyService)
    {
        _elementId = elementId;
        _scopedService = scopedService;
    }

    protected override Task<Paginated<TaskInstance>> FetchAsync(PaginationOptions options)
        => _scopedService.GetHistoryAsync(_elementId, options);
}
