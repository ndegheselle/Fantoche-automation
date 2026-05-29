using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Execution;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Views.WorkPages.Scopes
{
    /// <summary>
    /// MIGRATION: paged task-instance history for a scope. Replaces the old code-behind that called
    /// ScopesClient + bound a ListPageWrapper directly. Loads via <see cref="IScopesService.GetInstancesAsync"/>.
    /// </summary>
    public partial class ScopeHistoryViewModel : ObservableObject
    {
        private readonly IScopesService _scopes;

        [ObservableProperty]
        private IReadOnlyList<TaskInstance> _instances = [];

        [ObservableProperty]
        private int _total;

        [ObservableProperty]
        private int _page = 1;

        [ObservableProperty]
        private int _pageSize = 50;

        public Guid ScopeId { get; set; }

        public ScopeHistoryViewModel(IScopesService scopes) => _scopes = scopes;

        public async Task LoadAsync(int page, int pageSize)
        {
            if (ScopeId == Guid.Empty)
                return;

            try
            {
                // The service paginates 0-based; the pager is 1-based.
                var result = await _scopes.GetInstancesAsync(ScopeId, page - 1, pageSize);
                Instances = result.Data;
                Total = (int)result.Total;
                Page = page;
                PageSize = pageSize;
            }
            catch (NotImplementedException)
            {
                Instances = [];
                Total = 0;
            }
        }
    }
}
