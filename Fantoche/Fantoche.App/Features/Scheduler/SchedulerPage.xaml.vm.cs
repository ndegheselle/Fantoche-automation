using Fantoche.App.Common;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;

namespace Fantoche.App.Features.Scheduler
{
    /// <summary>A schedule of a task or workflow, as the scheduler page lists it.</summary>
    public record ScheduledRow(string Name, EnumScopedType Type, string Cron, DateTime? NextRun, string Parameters);

    /// <summary>
    /// Every schedule there is, and how their runs spread over the next days. The schedules are
    /// edited on the task or workflow they start.
    /// </summary>
    public partial class SchedulerViewModel : ObservableObject
    {
        public const int Days = 7;

        public IReadOnlyList<int> Hours { get; } = [.. Enumerable.Range(0, 24)];

        [ObservableProperty] private List<ScheduledRow> _rows = [];
        [ObservableProperty] private List<DistributionDay> _distribution = [];
        [ObservableProperty] private string _summary = "";

        private readonly IScopedService _scoped;

        public SchedulerViewModel(IScopedService scoped)
        {
            _scoped = scoped;
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            List<ScopedElement> elements = await _scoped.GetTreeAsync();
            List<(BaseAutomationTask Task, Schedule Schedule)> schedules = [.. elements
                .OfType<BaseAutomationTask>()
                .SelectMany(task => task.Schedules.Select(schedule => (task, schedule)))];

            DateTime now = DateTime.Now;
            Rows = [.. schedules
                .Select(x => new ScheduledRow(
                    x.Task.Metadata.Name,
                    x.Task.Metadata.Type,
                    x.Schedule.Cron,
                    x.Schedule.NextOccurrence(now),
                    Json.Parse(x.Schedule.ParametersJson)?.ToString(Formatting.None) ?? ""))
                .OrderBy(x => x.NextRun ?? DateTime.MaxValue)];

            Distribution = ScheduleDistribution.Build(schedules.Select(x => (x.Task.Metadata.Name, x.Schedule)), now, Days);
            Summary = SummaryOf(schedules.Count, Distribution);
        }

        private static string SummaryOf(int schedules, List<DistributionDay> distribution)
        {
            var cells = distribution.SelectMany(day => day.Hours.Select(hour => (day.Date, Cell: hour))).ToList();
            int runs = cells.Sum(x => x.Cell.Count);
            string text = $"{schedules} schedule{(schedules == 1 ? "" : "s")}, {runs} run{(runs == 1 ? "" : "s")} over the next {Days} days";
            if (runs == 0)
                return text;

            var busiest = cells.MaxBy(x => x.Cell.Count);
            return $"{text}, the busiest hour being {busiest.Date.AddHours(busiest.Cell.Hour):ddd dd MMM HH}:00 ({busiest.Cell.Count} runs)";
        }
    }
}
