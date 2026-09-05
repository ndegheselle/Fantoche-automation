using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Shared.Data.Graph
{
    /// <summary>
    /// The graph as it would run, known without running it : what every node hands over is built
    /// from the schemas it declares and from the mappings the graph is made of, so an editor can
    /// show what a node reads, check what it writes and deduce the schemas of the workflow.
    /// <para>
    /// The contexts it builds are the very ones a run builds (see <see cref="GraphContext"/>), only
    /// filled with samples : a task stands for a sample of its output schema, a control for its
    /// mapping resolved against what reaches it. What differs from a run is the walk — backwards,
    /// exhaustive and terminating on its own — because an editor answers for any node whether or not
    /// a run would ever reach it.
    /// </para>
    /// <para>
    /// The samples are computed once per node and kept, a graph being walked many times over the
    /// course of an edition : an instance lives as long as the edition it serves, never longer.
    /// </para>
    /// </summary>
    public class GraphSampling
    {
        private readonly AutomationWorkflow _workflow;
        private readonly TasksGraph _graph;
        private readonly JToken? _global;

        /// <summary>What a node hands over, once computed.</summary>
        private readonly Dictionary<Guid, JToken?> _outputs = [];

        /// <summary>What "$shared" holds for a node, once computed.</summary>
        private readonly Dictionary<Guid, JToken?> _shared = [];

        /// <summary>
        /// The nodes being computed : a graph can loop back on itself, and a node feeding itself
        /// stands for nothing more than what the rest of the branch already says.
        /// </summary>
        private readonly HashSet<Guid> _walking = [];

        /// <summary>
        /// What is wrong with the graph itself rather than with one of its mappings : two shares
        /// disagreeing on the type of a value. Filled as the samples are built.
        /// </summary>
        private List<string> Errors { get; } = [];

        public GraphSampling(AutomationWorkflow workflow, JToken? global = null)
        {
            _workflow = workflow;
            _graph = workflow.Graph;
            _global = global;

            // A graph read from the storage only holds the ids of what it connects : walking it
            // backwards needs the connectors wired to their node. Refreshing an already refreshed
            // graph does nothing, so an editor keeps the tasks it loaded.
            _graph.Refresh();
        }

        #region Contexts

        /// <summary>
        /// The contexts [node] can run with, one per branch reaching it : at runtime "$previous" is
        /// what a single branch produced, whichever led there. A node merging its branches has a
        /// single context instead, holding them all indexed by node name.
        /// </summary>
        public IReadOnlyList<GraphContext> GetContexts(BaseGraphTask node)
        {
            List<BaseGraphTask> previous = [.. GetEffectivePrevious(node)];
            JToken? shared = GetSharedSample(node);

            if (node is GraphControl control && control.IsWaiting(_workflow.WorkflowSettings.StopAtFirstEnd))
                return [GraphContext.From(previous.ToDictionary(x => x.Name, GetOutputSample), shared, _global)];

            if (previous.Count == 0)
                return [GraphContext.From(null, null, shared, _global)];

            return [.. previous.Select(x => GraphContext.From(x.Name, GetOutputSample(x), shared, _global))];
        }

        /// <summary>
        /// The nodes [node] actually reads : a node passing through produces nothing of its own, so
        /// what feeds it is what feeds the ones before it.
        /// </summary>
        private IEnumerable<BaseGraphTask> GetEffectivePrevious(BaseGraphTask node)
            => _graph.GetPrevious(node)
                .SelectMany(_graph.GetEffective)
                .DistinctBy(x => x.Id);

        #endregion

        #region Samples

        /// <summary>
        /// What [node] hands over to the ones after it, null when it produces nothing.
        /// </summary>
        public JToken? GetOutputSample(BaseGraphTask node)
        {
            if (_outputs.TryGetValue(node.Id, out JToken? cached))
                return cached;

            // The graph loops back on this node : what it produces is being computed higher up the
            // walk, taking it into account again would never end.
            if (!_walking.Add(node.Id))
                return null;

            try
            {
                JToken? sample = ComputeOutputSample(node);
                _outputs[node.Id] = sample;
                return sample;
            }
            finally
            {
                _walking.Remove(node.Id);
            }
        }

        private JToken? ComputeOutputSample(BaseGraphTask node)
        {
            if (node is GraphControl control)
            {
                // The start hands over what the workflow is started with : the shape comes from the
                // schema, and the default values are what is known of it before it runs. Resolved
                // the way the run resolves them, so a default pointing at "$global" reads alike.
                if (control.IsStart())
                    return GraphContext.ApplyDefaults(Sample(_workflow.InputSchemaJson), ResolveMapping(node));

                // The share feeds the shared context and lets the branch through untouched.
                if (control.IsShare())
                    return GetPreviousSample(node);

                return ResolveMapping(node);
            }

            // A task producing nothing of its own is transparent to the ones after it.
            if (node.AutomationTask?.Settings.IsPassingThrough == true)
                return GetPreviousSample(node);

            return Sample(node.OutputSchemaJson);
        }

        /// <summary>
        /// What reaches [node], every branch merged : for a node handing over what it reads, that is
        /// what it hands over.
        /// </summary>
        private JToken? GetPreviousSample(BaseGraphTask node)
        {
            JToken? sample = null;
            foreach (BaseGraphTask previous in GetEffectivePrevious(node))
                sample = GraphContext.Merge(sample, GetOutputSample(previous));
            return sample;
        }

        /// <summary>
        /// The mapping of [node] resolved against every context it can run with, merged : what the
        /// node produces whichever branch led to it.
        /// </summary>
        public JToken? ResolveMapping(BaseGraphTask node)
        {
            JToken? resolved = null;
            foreach (GraphContext context in GetContexts(node))
                resolved = GraphContext.Merge(resolved, node.ResolveInputMapping(context));
            return resolved;
        }

        #endregion

        #region Shared

        /// <summary>
        /// What "$shared" holds where [node] runs : every share leading to it, merged. A null node
        /// stands for the whole workflow, which is what it holds by the time it is over.
        /// </summary>
        public JToken? GetSharedSample(BaseGraphTask? node)
        {
            Guid key = node?.Id ?? Guid.Empty;
            if (_shared.TryGetValue(key, out JToken? cached))
                return cached;

            // Held before it is computed : a share reads the shared context of the ones before it,
            // never its own.
            _shared[key] = null;

            JObject shared = [];
            foreach (GraphControl share in GetShares(node))
            {
                if (ResolveMapping(share) is not JObject contribution)
                    continue;
                Merge(shared, contribution, share.Name, path: string.Empty);
            }

            _shared[key] = shared;
            return shared;
        }

        /// <summary>
        /// The shares [node] runs after, or every share of the graph when no node is given. A loop
        /// is walked once : what a share wrote a turn earlier is readable the next one.
        /// </summary>
        private IEnumerable<GraphControl> GetShares(BaseGraphTask? node)
        {
            if (node == null)
                return _graph.Nodes.OfType<GraphControl>().Where(x => x.IsShare());

            List<GraphControl> shares = [];
            HashSet<Guid> visited = [node.Id];
            Queue<BaseGraphTask> pending = new(_graph.GetPrevious(node));

            while (pending.Count > 0)
            {
                BaseGraphTask current = pending.Dequeue();
                if (!visited.Add(current.Id))
                    continue;

                if (current is GraphControl control && control.IsShare())
                    shares.Add(control);

                foreach (BaseGraphTask previous in _graph.GetPrevious(current))
                    pending.Enqueue(previous);
            }

            return shares;
        }

        /// <summary>
        /// Add what [source] writes to the shared context, an existing value being kept when the two
        /// disagree : a value can't be of two types depending on the branch that ran.
        /// </summary>
        private void Merge(JObject shared, JObject contribution, string source, string path)
        {
            foreach (JProperty property in contribution.Properties())
            {
                string name = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                JToken? existing = shared[property.Name];

                if (existing == null)
                {
                    shared[property.Name] = property.Value.DeepClone();
                    continue;
                }

                if (existing is JObject nested && property.Value is JObject values)
                {
                    Merge(nested, values, source, name);
                    continue;
                }

                if (existing.Type != property.Value.Type)
                    Errors.Add($"The shared value '{name}' is set as {existing.Type} and as {property.Value.Type} by '{source}'.");
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// What is wrong with [inputMappingJson] for [node], empty when the mapping holds up. Every
        /// branch reaching the node is checked on its own and named in the errors it raises : the
        /// mapping has to hold up whichever branch leads there.
        /// <para>
        /// The mapping is taken as a parameter rather than read from the node : an editor checks
        /// what is being typed, which isn't what the graph holds yet.
        /// </para>
        /// </summary>
        /// <param name="expected">
        /// Schema the resolved mapping has to match, null when the node isn't expected to produce a
        /// shape (the references are then the only thing checked).
        /// </param>
        /// <param name="partial">
        /// Whether a value the schema requires may be missing : the default values of a start only
        /// stand for what the caller doesn't give, so they are never complete on their own.
        /// </param>
        public List<string> Validate(
            BaseGraphTask node,
            string? inputMappingJson,
            JsonSchema? expected,
            bool partial = false)
        {
            if (partial)
                expected = WithoutRequired(expected);

            if (string.IsNullOrWhiteSpace(inputMappingJson))
            {
                // Nothing is handed over at runtime : an empty mapping only holds up when the node
                // is expected to produce nothing at all.
                if (expected == null)
                    return [];

                var missing = expected.Validate(JValue.CreateNull());
                if (missing.Count == 0)
                    return [];

                return [$"No mapping while the task expects one ({string.Join(", ", missing.Select(x => x.Kind).Distinct())})."];
            }

            JToken template;
            try
            {
                template = JToken.Parse(inputMappingJson);
            }
            catch
            {
                // Not JSON yet : what it is made of is checked where it is edited.
                return [];
            }

            List<string> errors = [];
            IReadOnlyList<GraphContext> contexts = GetContexts(node);

            foreach (GraphContext context in contexts)
            {
                List<string> branchErrors = [];

                ReferenceReplaceContext resolution = context.Report(template);
                foreach (ReferenceReplaceError error in resolution.Errors)
                    branchErrors.Add(error.ToString());

                if (expected != null)
                {
                    foreach (var error in expected.Validate(resolution.ReplacedSetting))
                        branchErrors.Add($"{error.Path} : {error.Kind}");
                }

                // The branch is only worth naming when there is more than one way in.
                foreach (string error in branchErrors)
                    errors.Add(context.Branch == null || contexts.Count == 1 ? error : $"[{context.Branch}] {error}");
            }

            return [.. errors.Distinct()];
        }

        /// <summary>
        /// A copy of [schema] holding the same shape and the same types, but requiring nothing : the
        /// values it describes may be given later.
        /// </summary>
        private static JsonSchema? WithoutRequired(JsonSchema? schema)
        {
            if (schema == null)
                return null;

            JsonSchema copy;
            try
            {
                copy = JsonSchema.FromJsonAsync(schema.ToJson()).Result;
            }
            catch
            {
                return schema;
            }

            ClearRequired(copy, []);
            return copy;
        }

        private static void ClearRequired(JsonSchema schema, HashSet<JsonSchema> visited)
        {
            if (!visited.Add(schema))
                return;

            schema.RequiredProperties.Clear();

            foreach (JsonSchemaProperty property in schema.Properties.Values)
            {
                property.IsRequired = false;
                ClearRequired(property, visited);
            }

            foreach (JsonSchema definition in schema.Definitions.Values)
                ClearRequired(definition, visited);

            if (schema.Item != null)
                ClearRequired(schema.Item, visited);
        }

        #endregion

        #region Schemas

        /// <summary>
        /// The schema [sample] is an example of, null when there is nothing to describe. What is
        /// deduced holds the shape and the types, nothing of what only a hand written schema says
        /// (what is required, the formats, the allowed values).
        /// </summary>
        private static JsonSchema? Deduce(JToken? sample)
        {
            if (sample == null || sample.Type == JTokenType.Null)
                return null;

            try
            {
                return JsonSchema.FromSampleJson(sample.ToString());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Write on the workflow the schemas its graph deduces : what every control produces, the
        /// output of the workflow and its shared values. They are stored so that a caller knows what
        /// a workflow reads and produces without loading its graph, but they are never edited by
        /// hand : the mappings are what they are read from. Returns what makes the graph invalid.
        /// <para>
        /// Called through <see cref="AutomationWorkflow.DeriveSchemas"/>.
        /// </para>
        /// </summary>
        public List<string> DeriveSchemas()
        {
            foreach (GraphControl control in _graph.Nodes.OfType<GraphControl>())
            {
                // The start hands over the input of the workflow, which is the one schema written by
                // hand ; the connectors of a node are set once, so only its output is refreshed.
                control.OutputSchemaJson = control.IsStart()
                    ? _workflow.InputSchemaJson
                    : Deduce(GetOutputSample(control))?.ToJson();
            }

            GraphControl? end = _graph.GetEndNodes().FirstOrDefault();
            _workflow.OutputSchemaJson = end == null ? null : Deduce(ResolveMapping(end))?.ToJson();
            _workflow.SharedSchemaJson = Deduce(GetSharedSample(null))?.ToJson();

            // The same disagreement is met once per node reading the shared values, and is worth
            // being told once.
            return [.. Errors.Distinct()];
        }

        /// <summary>
        /// An example of what [schemaJson] describes, null when there is no schema.
        /// </summary>
        private static JToken? Sample(string? schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson))
                return null;

            try
            {
                return JsonSchema.FromJsonAsync(schemaJson).Result.ToSampleJson();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
