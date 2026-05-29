using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Execution;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Views.WorkPages.Tasks
{
    /// <summary>
    /// MIGRATION: paged task-instance history for a task (parallels ScopeHistoryViewModel).
    /// Loads via <see cref="ITasksService.GetInstancesAsync"/>.
    /// </summary>
    public partial class TaskHistoryViewModel : ObservableObject
    {
        private readonly ITasksService _tasks;

        [ObservableProperty]
        private IReadOnlyList<TaskInstance> _instances = [];

        [ObservableProperty]
        private int _total;

        [ObservableProperty]
        private int _page = 1;

        [ObservableProperty]
        private int _pageSize = 50;

        public Guid TaskId { get; set; }

        public TaskHistoryViewModel(ITasksService tasks) => _tasks = tasks;

        public async Task LoadAsync(int page, int pageSize)
        {
            if (TaskId == Guid.Empty)
                return;

            try
            {
                var result = await _tasks.GetInstancesAsync(TaskId, page - 1, pageSize);
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
