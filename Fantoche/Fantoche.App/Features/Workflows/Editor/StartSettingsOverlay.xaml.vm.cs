using System.Collections.ObjectModel;
using Fantoche.App.Common;
using Fantoche.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Data.Model;
using Joufflu.Navigation;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.App.Features.Workflows.Editor
{
    /// <summary>
    /// Settings a workflow is started with : the input the executor validates against the
    /// <see cref="BaseAutomationTask.InputSchema"/> of the workflow, filled in as a tree built from
    /// that schema, next to the schema itself.
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
        /// The settings the run is started with, prefilled with the defaults of the start and with an
        /// empty value for the rest, so there is only the values left to fill in.
        /// </summary>
        public DataObject Settings { get; }

        /// <summary>
        /// What a field can be forced to : the references the defaults of the start hold, so they
        /// are handed over as written rather than lost.
        /// </summary>
        public IReadOnlyList<DataManualValue> References { get; }

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

            JToken? defaults = Defaults(workflow);
            References = [.. Json.ReferencesIn(defaults).Distinct().Select(reference => new DataManualValue(null, reference))];
            Settings = FromSchema(workflow) ?? new DataObject(null);

            // The defaults are displayed rather than left out : what is filled in here wins over them
            // (see GraphContextResolution.MergeContexts), so an empty value would replace one.
            var values = (JObject)Settings.ToToken()!;
            if (defaults != null)
                values.Merge(defaults, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            Settings.Load(values, References);

            Errors.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasErrors));
                StartCommand.NotifyCanExecuteChanged();
            };
            Settings.Changed += (_, _) => Refresh();

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
        /// Check that the settings match what the workflow expects, so a run isn't started just to
        /// fail on its first node.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

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

            if (schema != null && !schema.ActualSchema.IsObject)
            {
                Errors.Add("Schema : the workflow has to be started with an object.");
                return;
            }

            foreach (var error in schema?.Validate(Settings.ToToken()!) ?? [])
                Errors.Add($"{error.Path} : {error.Kind}");
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start() => Close(Settings.ToToken());

        private bool CanStart() => !HasErrors;

        /// <summary>
        /// The tree of what [workflow] expects, null when its schema can't be read or describes no
        /// object : <see cref="Refresh"/> is where that is reported.
        /// </summary>
        private static DataObject? FromSchema(AutomationWorkflow workflow)
        {
            try
            {
                JsonSchema? schema = workflow.InputSchema?.ActualSchema;
                return schema?.IsObject == true ? schema.ToDataNode() as DataObject : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// What the start of [workflow] hands over for the values the caller doesn't give, null when
        /// it holds none or when what it holds can't be read.
        /// </summary>
        private static JToken? Defaults(AutomationWorkflow workflow)
        {
            try
            {
                return Json.Parse(workflow.Graph.GetStartNodes().FirstOrDefault()?.InputTemplateJson) as JObject;
            }
            catch (Exception)
            {
                // The settings of the workflow are where that is reported, not the start of a run.
                return null;
            }
        }
    }
}
