using System.Collections.ObjectModel;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// Settings a workflow is started with : the input the executor validates against the
    /// <see cref="BaseAutomationTask.InputSchema"/> of the workflow, edited as raw JSON next to the
    /// schema expecting it.
    /// <para>
    /// Only displayed when the workflow actually expects something, see
    /// <see cref="IsExpectingSettings"/> : a workflow taking no input is started right away.
    /// </para>
    /// </summary>
    public partial class StartSettingsViewModel : ObservableObject
    {
        public AutomationWorkflow Workflow { get; }

        public string Title { get; }

        /// <summary>
        /// Schema of what the workflow expects, displayed read only.
        /// </summary>
        public string SchemaJson { get; }

        /// <summary>
        /// The settings the run is started with, prefilled with an empty value per expected
        /// property so there is only the values left to type.
        /// </summary>
        [ObservableProperty] private string _settingsJson;

        /// <summary>
        /// What is wrong with the current settings, blocking the start while not empty.
        /// </summary>
        public ObservableCollection<string> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// The settings to start with, only set once they have been validated.
        /// </summary>
        public JToken? Settings { get; private set; }

        private readonly IOverlayService _overlays;

        public StartSettingsViewModel(AutomationWorkflow workflow, IOverlayService overlays)
        {
            Workflow = workflow;
            _overlays = overlays;
            Title = $"Start - {workflow.Metadata.Name}";
            SchemaJson = Format(workflow.InputSchemaJson);
            _settingsJson = BuildTemplate(workflow.InputSchema);

            Errors.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasErrors));
                StartCommand.NotifyCanExecuteChanged();
            };

            Refresh();
        }

        /// <summary>
        /// Whether [workflow] expects settings to be started with : it has an input schema, and that
        /// schema asks for something. A schema without any property (the default one of a workflow
        /// that was never given an input) leaves nothing to fill in.
        /// </summary>
        public static bool IsExpectingSettings(AutomationWorkflow workflow)
        {
            if (workflow.InputSchemaJson == null)
                return false;

            try
            {
                return workflow.InputSchema?.ActualProperties.Count > 0;
            }
            catch (Exception)
            {
                // A schema that can't even be read is worth displaying, the user being the one who
                // wrote it.
                return true;
            }
        }

        /// <summary>
        /// Ask for the settings of the next run of [workflow] and wait for the user to validate
        /// them, <see langword="null"/> being returned when the start is cancelled.
        /// </summary>
        public static async Task<JToken?> ShowAsync(AutomationWorkflow workflow)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            var viewModel = new StartSettingsViewModel(workflow, overlays);
            if (await overlays.Show(viewModel, new OverlayOptions() { Title = viewModel.Title }) != true)
                return null;
            return viewModel.Settings;
        }

        /// <summary>
        /// Check that the settings are valid JSON and that they match what the workflow expects, so
        /// a run isn't started just to fail on its first node.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

            JToken? settings;
            try
            {
                settings = JToken.Parse(SettingsJson);
            }
            catch (Exception exception)
            {
                Errors.Add($"Settings : {exception.Message}");
                return;
            }

            JsonSchema? schema;
            try
            {
                schema = Workflow.InputSchema;
            }
            catch (Exception exception)
            {
                Errors.Add($"Schema : {exception.Message}");
                return;
            }

            foreach (var error in schema?.Validate(settings) ?? [])
                Errors.Add($"{error.Path} : {error.Kind}");
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            Settings = JToken.Parse(SettingsJson);
            _overlays.CloseTop(true);
        }

        private bool CanStart() => !HasErrors;

        [RelayCommand]
        private void Cancel() => _overlays.CloseTop(false);

        partial void OnSettingsJsonChanged(string value) => Refresh();

        /// <summary>
        /// An object holding an empty value per expected property : what the workflow is asking for,
        /// left to fill in. Anything the schema doesn't describe as an object falls back on an empty
        /// object.
        /// </summary>
        private static string BuildTemplate(JsonSchema? schema)
        {
            var template = new JObject();
            if (schema != null)
            {
                foreach ((string name, JsonSchemaProperty property) in schema.ActualProperties)
                    template[name] = EmptyValue(property);
            }

            return template.ToString(Formatting.Indented);
        }

        private static JToken EmptyValue(JsonSchema schema)
        {
            if (schema.Default != null)
                return JToken.FromObject(schema.Default);

            JsonObjectType type = schema.Type;
            if (type.HasFlag(JsonObjectType.Integer) || type.HasFlag(JsonObjectType.Number))
                return 0;
            if (type.HasFlag(JsonObjectType.Boolean))
                return false;
            if (type.HasFlag(JsonObjectType.Array))
                return new JArray();
            if (type.HasFlag(JsonObjectType.Object))
                return new JObject();
            return "";
        }

        /// <summary>
        /// The schema as it is displayed : indented, or as it was written when it can't be read as
        /// JSON.
        /// </summary>
        private static string Format(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "";

            try
            {
                return JToken.Parse(json).ToString(Formatting.Indented);
            }
            catch (Exception)
            {
                return json;
            }
        }
    }
}
