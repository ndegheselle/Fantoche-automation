using Fantoche.App.Features.Workflows.Controls;
using Fantoche.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.Workflows.Editor
{
    /// <summary>
    /// Settings a workflow is started with, see <see cref="ParametersForm"/>.
    /// <para>
    /// Only displayed when the workflow actually expects something, see
    /// <see cref="ParametersForm.IsExpectingParameters"/> : a workflow taking no input is started
    /// right away.
    /// </para>
    /// </summary>
    public partial class StartSettingsViewModel : ObservableObject, IOverlayContent<JToken>
    {
        public AutomationWorkflow Workflow { get; }

        public OverlayOptions Options { get; }

        public JToken? Result => Settings.ToToken();

        public ParametersForm Settings { get; }

        private readonly IOverlayer _overlayer;

        public StartSettingsViewModel(AutomationWorkflow workflow, IOverlayer overlayer)
        {
            Workflow = workflow;
            _overlayer = overlayer;
            Options = new OverlayOptions() { Title = $"Start - {workflow.Metadata.Name}" };
            Settings = new ParametersForm(workflow);
            Settings.Errors.CollectionChanged += (_, _) => StartCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Ask for the settings of the next run of [workflow], <see langword="null"/> when cancelled.
        /// </summary>
        public static Task<JToken?> ShowAsync(AutomationWorkflow workflow)
            => SpineViewModel.Instance.Overlayer.ShowAsync(new StartSettingsViewModel(workflow, SpineViewModel.Instance.Overlayer));

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start() => _overlayer.Validate(this);

        [RelayCommand]
        private void Cancel() => _overlayer.Cancel(this);

        private bool CanStart() => !Settings.HasErrors;
    }
}
