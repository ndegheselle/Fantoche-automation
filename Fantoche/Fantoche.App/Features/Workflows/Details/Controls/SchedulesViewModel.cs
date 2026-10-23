using System.Collections.ObjectModel;
using Fantoche.App.Common;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;
using Newtonsoft.Json;

namespace Fantoche.App.Features.Workflows.Details.Controls
{
    /// <summary>A schedule as the schedules tab lists it.</summary>
    public record ScheduleRow(Schedule Schedule, DateTime? NextRun, string Parameters);

    /// <summary>
    /// The schedules of a task or workflow, displayed by its schedules tab. Each change is saved
    /// right away : a schedule is edited in an overlay of its own, already checked when it closes.
    /// </summary>
    public partial class SchedulesViewModel : ObservableObject
    {
        public ObservableCollection<ScheduleRow> Rows { get; } = [];

        private readonly BaseAutomationTask _task;
        private readonly IOverlayer _overlays;
        private readonly Func<Task> _saveAsync;

        /// <param name="saveAsync">Persist the element, its schedules with it.</param>
        public SchedulesViewModel(BaseAutomationTask task, IOverlayer overlays, Func<Task> saveAsync)
        {
            _task = task;
            _overlays = overlays;
            _saveAsync = saveAsync;
            Refresh();
        }

        [RelayCommand]
        private async Task Add()
        {
            Schedule? schedule = await ScheduleViewModel.ShowAsync(_task, null, _overlays);
            if (schedule == null)
                return;

            _task.Schedules.Add(schedule);
            await SaveAsync();
        }

        [RelayCommand]
        private async Task Edit(ScheduleRow row)
        {
            Schedule? schedule = await ScheduleViewModel.ShowAsync(_task, row.Schedule, _overlays);
            if (schedule == null)
                return;

            int index = _task.Schedules.IndexOf(row.Schedule);
            _task.Schedules[index] = schedule;
            await SaveAsync();
        }

        [RelayCommand]
        private async Task Remove(ScheduleRow row)
        {
            if (await _overlays.Confirm($"Remove the schedule '{row.Schedule.Cron}' ?", "Confirm removal", EnumConfirmationType.Danger) != true)
                return;

            _task.Schedules.Remove(row.Schedule);
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            Refresh();
            await _saveAsync();
        }

        private void Refresh()
        {
            Rows.Clear();
            foreach (Schedule schedule in _task.Schedules)
            {
                string parameters = Json.Parse(schedule.ParametersJson)?.ToString(Formatting.None) ?? "";
                Rows.Add(new ScheduleRow(schedule, schedule.NextOccurrence(DateTime.Now), parameters));
            }
        }
    }
}
