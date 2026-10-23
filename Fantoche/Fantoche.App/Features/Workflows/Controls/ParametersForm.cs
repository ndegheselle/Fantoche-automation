using System.Collections.ObjectModel;
using Fantoche.App.Common;
using Fantoche.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Data.Model;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.App.Features.Workflows.Controls
{
    /// <summary>
    /// The parameters a task or workflow is started with : the input the executor validates against
    /// its <see cref="BaseAutomationTask.InputSchema"/>, filled in as a tree built from that schema.
    /// Used to start a workflow and to schedule an element.
    /// </summary>
    public partial class ParametersForm : ObservableObject
    {
        /// <summary>
        /// The parameters, prefilled with the defaults of the start of a workflow and with an empty
        /// value for the rest, so there is only the values left to fill in.
        /// </summary>
        public DataObject Values { get; }

        /// <summary>
        /// What a field can be forced to : the references the defaults of the start hold, so they
        /// are handed over as written rather than lost.
        /// </summary>
        public IReadOnlyList<DataManualValue> References { get; }

        /// <summary>What is wrong with the current parameters, blocking their use while not empty.</summary>
        public ObservableCollection<string> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        private readonly BaseAutomationTask _task;

        /// <param name="values">Parameters to start from, over the defaults (e.g. those of a schedule being edited).</param>
        public ParametersForm(BaseAutomationTask task, JToken? values = null)
        {
            _task = task;

            JToken? defaults = Defaults(task);
            References = [.. Json.ReferencesIn(defaults).Concat(Json.ReferencesIn(values))
                .Distinct()
                .Select(reference => new DataManualValue(null, reference))];
            Values = FromSchema(task) ?? new DataObject(null);

            // The defaults are displayed rather than left out : what is filled in here wins over them
            // (see GraphContextResolution.MergeContexts), so an empty value would replace one.
            var merged = (JObject)Values.ToToken()!;
            var replaceArrays = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace };
            if (defaults != null)
                merged.Merge(defaults, replaceArrays);
            if (values != null)
                merged.Merge(values, replaceArrays);
            Values.Load(merged, References);

            Errors.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasErrors));
            Values.Changed += (_, _) => Refresh();

            Refresh();
        }

        /// <summary>
        /// Whether [task] expects parameters : it has an input schema, and that schema asks for
        /// something. A schema without any property (the default one of a workflow that was never
        /// given an input) leaves nothing to fill in.
        /// </summary>
        public static bool IsExpectingParameters(BaseAutomationTask task)
        {
            if (task.InputSchemaJson == null)
                return false;

            try
            {
                return task.InputSchema?.ActualProperties.Count > 0;
            }
            catch (Exception)
            {
                // A schema that can't even be read is worth displaying, the user being the one who
                // wrote it.
                return true;
            }
        }

        public JToken? ToToken() => Values.ToToken();

        /// <summary>
        /// Check that the parameters match what the element expects, so it isn't started just to fail
        /// on its first node.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

            JsonSchema? schema;
            try
            {
                schema = _task.InputSchema;
            }
            catch (Exception exception)
            {
                Errors.Add($"Schema : {exception.Message}");
                return;
            }

            if (schema != null && !schema.ActualSchema.IsObject)
            {
                Errors.Add("Schema : the element has to be started with an object.");
                return;
            }

            foreach (var error in schema?.Validate(Values.ToToken()!) ?? [])
                Errors.Add($"{error.Path} : {error.Kind}");
        }

        /// <summary>
        /// The tree of what [task] expects, null when its schema can't be read or describes no
        /// object : <see cref="Refresh"/> is where that is reported.
        /// </summary>
        private static DataObject? FromSchema(BaseAutomationTask task)
        {
            try
            {
                JsonSchema? schema = task.InputSchema?.ActualSchema;
                return schema?.IsObject == true ? schema.ToDataNode() as DataObject : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// What the start of a workflow hands over for the values the caller doesn't give, null for a
        /// task, when the start holds none or when what it holds can't be read.
        /// </summary>
        private static JToken? Defaults(BaseAutomationTask task)
        {
            if (task is not AutomationWorkflow workflow)
                return null;

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
