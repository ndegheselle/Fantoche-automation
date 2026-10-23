using Fantoche.App.Common;
using Fantoche.App.Features.Workflows.Controls;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json;

namespace Fantoche.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// A schedule of a task or workflow being added or edited : its cron, with the next runs it
    /// gives, and the parameters each run starts with.
    /// </summary>
    public partial class ScheduleViewModel : ObservableObject, IOverlayContent<Schedule>
    {
        private const int ShownRuns = 5;

        public OverlayOptions Options { get; }

        public Schedule? Result => new()
        {
            Cron = Cron.Trim(),
            ParametersJson = Parameters?.ToToken()?.ToString(Formatting.None),
        };

        /// <summary>Null when the element expects no parameters.</summary>
        public ParametersForm? Parameters { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private string _cron;

        /// <summary>Why <see cref="Cron"/> can't be used, null when it can.</summary>
        [ObservableProperty] private string? _cronError;

        /// <summary>The next runs the cron gives, so it reads as what it means.</summary>
        [ObservableProperty] private List<DateTime> _nextRuns = [];

        private readonly IOverlayer _overlayer;

        /// <param name="schedule">The schedule edited, null for a new one.</param>
        public ScheduleViewModel(BaseAutomationTask task, Schedule? schedule, IOverlayer overlayer)
        {
            _overlayer = overlayer;
            Options = new OverlayOptions() { Title = $"{(schedule == null ? "New schedule" : "Schedule")} - {task.Metadata.Name}" };
            _cron = schedule?.Cron ?? "0 6 * * *";

            if (ParametersForm.IsExpectingParameters(task))
            {
                Parameters = new ParametersForm(task, Json.Parse(schedule?.ParametersJson));
                Parameters.Errors.CollectionChanged += (_, _) => ValidateCommand.NotifyCanExecuteChanged();
            }

            RefreshCron();
        }

        /// <summary>
        /// Ask for a schedule of [task], starting from [schedule] when one is edited. Null when
        /// cancelled.
        /// </summary>
        public static Task<Schedule?> ShowAsync(BaseAutomationTask task, Schedule? schedule, IOverlayer overlayer)
            => overlayer.ShowAsync(new ScheduleViewModel(task, schedule, overlayer));

        partial void OnCronChanged(string value) => RefreshCron();

        private void RefreshCron()
        {
            var schedule = new Schedule { Cron = Cron.Trim() };
            CronError = Schedule.Validate(schedule.Cron);
            NextRuns = [.. schedule.OccurrencesBetween(DateTime.Now, DateTime.Now.AddYears(1)).Take(ShownRuns)];
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private void Validate() => _overlayer.Validate(this);

        [RelayCommand]
        private void Cancel() => _overlayer.Cancel(this);

        private bool CanValidate() => Schedule.Validate(Cron.Trim()) == null && Parameters?.HasErrors != true;
    }
}
