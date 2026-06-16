using System;
using System.Threading.Tasks;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;

namespace Automation.App.Features.History;

/// <summary>
/// View model for the full execution-history page. Shows every task instance, most recent first.
/// </summary>
internal partial class HistoryPageVm : HistoryVmBase, INavigable
{
    public HistoryPageVm(IHistoryService historyService) : base(historyService)
    {
    }

    protected override Task<Paginated<TaskInstance>> FetchAsync(PaginationOptions options)
        => HistoryService.SearchAsync(options);
}

/// <summary>
/// Design class for [HistoryPageVm]
/// </summary>
internal class HistoryPageVMDesign : HistoryPageVm
{
    public HistoryPageVMDesign() : base(null!)
    {
        var now = DateTime.UtcNow;
        Items.Add(new TaskInstance
        {
            NodeName = "Fetch files",
            CreatedAt = now.AddMinutes(-3),
            FinishedAt = now.AddMinutes(-2),
            State = EnumTaskState.Completed,
        });
        Items.Add(new TaskInstance
        {
            NodeName = "Parse data",
            CreatedAt = now.AddMinutes(-2),
            FinishedAt = now.AddMinutes(-1),
            State = EnumTaskState.Failed,
        });
        Items.Add(new TaskInstance
        {
            NodeName = "Import to database",
            CreatedAt = now.AddSeconds(-20),
            State = EnumTaskState.Progressing,
        });

        TotalItems = 3;
        IsLoading = false;
    }
}
