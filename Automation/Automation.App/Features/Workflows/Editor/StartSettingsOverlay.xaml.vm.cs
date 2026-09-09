using System.Collections.ObjectModel;
using Automation.App.Common;
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
    public partial class StartSettingsViewModel : OverlayViewModel<JToken>
    {
        public AutomationWorkflow Workflow { get; }

        /// <summary>Schema of what the workflow expects, displayed read only.</summary>
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

        public StartSettingsViewModel(AutomationWorkflow workflow, IOverlayService overlays)
            : base(overlays)
        {
            Workflow = workflow;
            Options.Title = $"Start - {workflow.Metadata.Name}";
            SchemaJson = Json.Format(workflow.InputSchemaJson);
            _settingsJson = BuildTemplate(workflow.InputSchema, Defaults(workflow));

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
        /// Ask for the settings of the next run of [workflow], <see langword="null"/> when cancelled.
        /// </summary>
        public static Task<JToken?> ShowAsync(AutomationWorkflow workflow)
            => ShowAsync(new StartSettingsViewModel(workflow, SpineViewModel.Instance.Overlays));

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
        private void Start() => Close(JToken.Parse(SettingsJson));

        private bool CanStart() => !HasErrors;

        partial void OnSettingsJsonChanged(string value) => Refresh();

        /// <summary>
        /// An object holding an empty value per expected property : what the workflow is asking for,
        /// left to fill in. Anything the schema doesn't describe as an object falls back on an empty
        /// object.
        /// </summary>
        private static string BuildTemplate(JsonSchema? schema, JObject? defaults)
        {
            var template = new JObject();
            if (schema != null)
            {
                // The default values are displayed rather than left out : what is typed here wins
                // over them (see <see cref="AutomationWorkflow.ApplyInputDefaults"/>), so an empty
                // placeholder would silently replace a default with nothing.
                foreach ((string name, JsonSchemaProperty property) in schema.ActualProperties)
                    template[name] = defaults?[name]?.DeepClone() ?? EmptyValue(property);
            }

            return template.ToString(Formatting.Indented);
        }

        /// <summary>
        /// What the start of [workflow] hands over for the values the caller doesn't give, null when
        /// it holds none or when what it holds can't be read.
        /// </summary>
        private static JObject? Defaults(AutomationWorkflow workflow)
        {
            try
            {
                return workflow.Graph.GetStartNodes().FirstOrDefault()?.InputTemplate as JObject;
            }
            catch (Exception)
            {
                // The settings of the workflow are where that is reported, not the start of a run.
                return null;
            }
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
    }
}
