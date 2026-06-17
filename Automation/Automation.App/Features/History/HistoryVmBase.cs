using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.History;

/// <summary>
/// Shared logic behind the <see cref="HistoryTable"/> control: a paginated, auto-refreshing list of
/// <see cref="TaskInstance"/>. Subclasses only provide the actual data source through
/// <see cref="FetchAsync"/> (the whole history, or the history of a single scoped element).
/// </summary>
internal abstract partial class HistoryVmBase : ObservableObject
{
    private bool _suppressReload;
    private bool _started;
    private CancellationTokenSource? _cts;

    /// <summary>History service used to refresh the list when executions come and go.</summary>
    protected IHistoryService HistoryService { get; }

    protected HistoryVmBase(IHistoryService historyService)
    {
        HistoryService = historyService;
    }

    public ObservableCollection<TaskInstance> Items { get; } = new();

    [ObservableProperty] private long _totalItems;

    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private int _pageSize = 100;

    [ObservableProperty] private bool _isLoading;

    /// <summary>Fetch a page of instances from the underlying source.</summary>
    protected abstract Task<Paginated<TaskInstance>> FetchAsync(PaginationOptions options);

    /// <summary>
    /// Begin showing the history: load the first page and listen for live changes. Driven by the
    /// <see cref="HistoryTable"/> control's visual lifecycle, so it is safe to call more than once.
    /// </summary>
    public void Start()
    {
        if (_started)
            return;
        _started = true;

        HistoryService.InstanceAdded += OnHistoryChanged;
        HistoryService.InstanceUpdated += OnHistoryChanged;
        _ = RefreshAsync();
    }

    /// <summary>Stop listening and cancel any in-flight refresh.</summary>
    public void Stop()
    {
        if (!_started)
            return;
        _started = false;

        HistoryService.InstanceAdded -= OnHistoryChanged;
        HistoryService.InstanceUpdated -= OnHistoryChanged;
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
        => System.Windows.Application.Current.Dispatcher.BeginInvoke(() => _ = RefreshAsync());

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

            var result = await FetchAsync(options);

            // FetchAsync has no CancellationToken, so we can't abort the call
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
