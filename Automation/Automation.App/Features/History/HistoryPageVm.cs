using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.History;

internal partial class HistoryPageVm : ObservableObject, INavigable
{
    private readonly IHistoryService _historyService;

    private bool _suppressReload;
    private CancellationTokenSource? _cts;

    public HistoryPageVm(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    public ObservableCollection<TaskInstance> Items { get; } = new();

    [ObservableProperty] private long _totalItems;

    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private int _pageSize = 100;

    [ObservableProperty] private bool _isLoading;

    public void OnShow()
    {
        _historyService.InstanceAdded += OnHistoryChanged;
        _historyService.InstanceUpdated += OnHistoryChanged;
        _ = RefreshAsync();
    }

    public void OnHide()
    {
        _historyService.InstanceAdded -= OnHistoryChanged;
        _historyService.InstanceUpdated -= OnHistoryChanged;
        _cts?.Cancel();
    }

    partial void OnPageSizeChanged(int value) => ResetToFirstPageAndReload();

    partial void OnCurrentPageChanged(int value)
    {
        if (_suppressReload)
            return;
        _ = RefreshAsync();
    }

    // Events come from a background timer thread, marshal back to the UI thread.
    private void OnHistoryChanged(TaskInstance instance)
        => Dispatcher.UIThread.Post(() => _ = RefreshAsync());

    private void ResetToFirstPageAndReload()
    {
        // Setting CurrentPage = 1 would normally fire OnCurrentPageChanged and
        // reload; suppress that so we only fetch once below.
        _suppressReload = true;
        CurrentPage = 1;
        _suppressReload = false;

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        // Cancel any in-flight refresh so a slow earlier response can't
        // overwrite a newer one.
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;

        try
        {
            IsLoading = true;

            var options = new PaginationOptions
            {
                Page = CurrentPage,
                PageSize = PageSize,
            };

            var result = await _historyService.SearchAsync(options);

            // SearchAsync has no CancellationToken, so we can't abort the call
            // itself — just discard the result if a newer request superseded us.
            if (token.IsCancellationRequested)
                return;

            TotalItems = result.Total;

            Items.Clear();
            foreach (var instance in result.Items)
                Items.Add(instance);
        }
        finally
        {
            // Only the most recent refresh clears the flag.
            if (ReferenceEquals(_cts, cts))
                IsLoading = false;
        }
    }
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
