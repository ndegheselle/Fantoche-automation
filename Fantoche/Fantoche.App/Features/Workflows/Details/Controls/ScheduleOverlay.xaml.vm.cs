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
    /// gives, and the parameters each run starts with, filled in with the interface of the element or
    /// the generated form (see <see cref="ParametersInput"/>).
    /// </summary>
    public partial class ScheduleViewModel : ObservableObject, IOverlayContent<Schedule>
    {
        private const int ShownRuns = 5;

        public OverlayOptions Options { get; }

        public Schedule? Result { get; private set; }

        /// <summary>Null when the element expects no parameters.</summary>
        public ParametersInput? Parameters { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private string _cron;

        /// <summary>Why <see cref="Cron"/> can't be used, null when it can.</summary>
        [ObservableProperty] private string? _cronError;

        /// <summary>The next runs the cron gives, so it reads as what it means.</summary>
        [ObservableProperty] private List<DateTime> _nextRuns = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private bool _isReading;

        private readonly IOverlayer _overlayer;

        /// <param name="schedule">The schedule edited, null for a new one.</param>
        /// <param name="parameters">Null when the element expects no parameters.</param>
        private ScheduleViewModel(BaseAutomationTask task, Schedule? schedule, ParametersInput? parameters, IOverlayer overlayer)
        {
            _overlayer = overlayer;
            Options = new OverlayOptions() { Title = $"{(schedule == null ? "New schedule" : "Schedule")} - {task.Metadata.Name}" };
            _cron = schedule?.Cron ?? "0 6 * * *";

            Parameters = parameters;
            if (parameters?.Form != null)
                parameters.Form.Errors.CollectionChanged += (_, _) => ValidateCommand.NotifyCanExecuteChanged();

            RefreshCron();
        }

        /// <summary>
        /// Ask for a schedule of [task], starting from [schedule] when one is edited. Null when
        /// cancelled.
        /// </summary>
        public static async Task<Schedule?> ShowAsync(BaseAutomationTask task, Schedule? schedule, IOverlayer overlayer)
        {
            ParametersInput? parameters = ParametersForm.IsExpectingParameters(task)
                ? await ParametersInput.CreateAsync(task, Json.Parse(schedule?.ParametersJson))
                : null;
            return await overlayer.ShowAsync(new ScheduleViewModel(task, schedule, parameters, overlayer));
        }

        partial void OnCronChanged(string value) => RefreshCron();

        private void RefreshCron()
        {
            var schedule = new Schedule { Cron = Cron.Trim() };
            CronError = Schedule.Validate(schedule.Cron);
            NextRuns = [.. schedule.OccurrencesBetween(DateTime.Now, DateTime.Now.AddYears(1)).Take(ShownRuns)];
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private async Task Validate()
        {
            IsReading = true;
            try
            {
                string? parametersJson = null;
                if (Parameters != null)
                {
                    ParametersAnswer answer = await Parameters.ReadAsync();
                    if (answer.IsRefused)
                        return;
                    parametersJson = answer.Parameters?.ToString(Formatting.None);
                }

                Result = new Schedule { Cron = Cron.Trim(), ParametersJson = parametersJson };
                _overlayer.Validate(this);
            }
            finally
            {
                IsReading = false;
            }
        }

        [RelayCommand]
        private void Cancel() => _overlayer.Cancel(this);

        private bool CanValidate() => !IsReading && Schedule.Validate(Cron.Trim()) == null && Parameters?.HasFormErrors != true;
    }
}
