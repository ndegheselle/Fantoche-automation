using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Graph;

public class GraphExecutionContext
{
    private const string PreviousIdentifier = "previous";
    private const string SharedIdentifier = "shared";
    private const string GlobalIdentifier = "global";

    private readonly TasksGraph _graph;
    private readonly WorkflowInstance _workflowInstance;

    public GraphExecutionContext(TasksGraph graph, JToken globalContext)
    {
        _graph = graph;
        _globalContext = globalContext;
    }

    // Get context per instances -> Caller responsability to resolve 

    public JToken GetContextFor(BaseGraphTask task, TaskInstance? previousInstance = null)
    {
        if (previousInstance != null)
        {
            return new JObject
            {
                [PreviousIdentifier] = previousInstance?.Output,
                [SharedIdentifier] = _workflowInstance.SharedContext,
                [GlobalIdentifier] = _workflowInstance.GlobalContext,
            };
        }

        var previous = _graph.GetPrevious(task).DistinctBy(x => x.TaskId);
        // Return a list of potential context (one per app)

        return new JObject
        {
            [PreviousIdentifier] = previousInstance?.Output,
            [SharedIdentifier] = _workflowInstance.Workflow.SharedSchema?.ToSampleJson(),
            [GlobalIdentifier] = _workflowInstance.GlobalContext,
        };
    }


    public List<string, JToken> GetAllContextFor(BaseGraphTask task, WorkflowInstance? workflowInstance = null)
    {

    }

    #region Samples

    /// <summary>
    /// Generate a sample of the contexts based on the previous tasks.
    /// Pass-through predecessors are transparently walked over: the sample is built
    /// from the nearest non-pass-through upstream node(s).
    /// </summary>
    /// <param name="task"></param>
    /// <param name="isWaitingAllInputs">
    /// Overrides <see cref="GraphTaskSettings.IsWaitingAllInputs"/>, so an editor can preview the
    /// samples of a setting the user hasn't applied to the task yet.
    /// </param>
    public List<string> GetContextSampleJsonFor(BaseGraphTask task, bool? isWaitingAllInputs = null)
    {
        JToken? context = GetContextSampleFor(task);

        List<string> contexts = [];
        if (isWaitingAllInputs ?? task.Settings.IsWaitingAllInputs)
        {
            contexts.Add(GenerateContextFrom(GetPreviousSamplesByName(task), context).ToString());
        }
        else
        {
            // XXX : maybe group by TaskId ?
            foreach (JToken? previous in GetPreviousSamples(task))
                contexts.Add(GenerateContextFrom(previous, context).ToString());
        }
        return contexts;
    }

    /// <summary>
    /// Sample of the output of each effective previous task, keyed by node name (shape used by the
    /// tasks waiting for all their inputs).
    /// </summary>
    private Dictionary<string, JToken?> GetPreviousSamplesByName(BaseGraphTask task)
    {
        Dictionary<string, JToken?> previous = [];
        foreach (var pre in _graph.GetPrevious(task))
            foreach (var effective in ResolveEffectivePreviousTasks(pre))
                previous[effective.Name] = effective.OutputSchema?.ToSampleJson();
        return previous;
    }

    /// <summary>
    /// Sample of the output of each effective previous task, one per branch reaching the task.
    /// </summary>
    private IEnumerable<JToken?> GetPreviousSamples(BaseGraphTask task)
    {
        foreach (var pre in _graph.GetPrevious(task))
            foreach (var effective in ResolveEffectivePreviousTasks(pre))
                yield return effective.OutputSchema?.ToSampleJson();
    }

    /// <summary>
    /// Walk back through pass-through nodes to the nearest non-pass-through ancestor(s).
    /// A pass-through doesn't contribute its own output to downstream contexts — the
    /// previous slot reads from whatever feeds it instead. Falls back to the node
    /// itself when it has no predecessors.
    /// </summary>
    private IEnumerable<BaseGraphTask> ResolveEffectivePreviousTasks(BaseGraphTask task, HashSet<Guid>? visited = null)
    {
        if (task.AutomationTask?.Settings.IsPassingThrough != true)
        {
            yield return task;
            yield break;
        }

        visited ??= [];
        if (!visited.Add(task.Id))
            yield break; // cycle guard

        var upstream = _graph.GetPrevious(task).ToList();
        if (upstream.Count == 0)
        {
            yield return task;
            yield break;
        }

        foreach (var pre in upstream)
            foreach (var resolved in ResolveEffectivePreviousTasks(pre, visited))
                yield return resolved;
    }

    /// <summary>
    /// Get context of all the end tasks since they act as one.
    /// </summary>
    /// <param name="isWaitingAllInputs">
    /// Overrides the setting of the end tasks, see <see cref="GetContextSampleJsonFor"/>.
    /// </param>
    /// <returns></returns>
    public List<string> GetContextSampleForEnd(bool? isWaitingAllInputs = null)
    {
        List<string> contexts = [];
        var endTasks = _graph.GetEndNodes();
        foreach (var task in endTasks) contexts.AddRange(GetContextSampleJsonFor(task, isWaitingAllInputs));
        return contexts;
    }

    /// <summary>
    /// Combine the outputs of all reached end node instances into a single workflow output token.
    /// </summary>
    public JToken? CombineEndOutputs(IReadOnlyList<TaskInstance> endInstances, WorkflowSettings settings)
    {
        if (endInstances.Count == 0)
            return null;

        // Each end instance carries its input as its output (see RunBranchAsync)
        // TODO : cancel all other current tasks (store cancelation token in instance ?)
        if (settings.StopAtFirstEnd)
            return endInstances.OrderBy(x => x.FinishedAt ?? x.CreatedAt).First().Output;

        if (endInstances.Count == 1)
            return endInstances[0].Output;

        // Merge object outputs together, fall back to an array for heterogeneous tokens
        if (endInstances.All(x => x.Output is JObject))
        {
            var merged = new JObject();
            foreach (var inst in endInstances)
                merged.Merge(inst.Output, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
            return merged;
        }

        return new JArray(endInstances.Select(x => x.Output).Where(x => x != null));
    }
    #endregion

    #region Context

    /// <summary>
    /// Context a task reads : the <see cref="ScopeContext"/> with the settings of every context
    /// setter placed upstream of it applied, in graph order. Unlike the previous outputs the context
    /// flows through every node, pass-through ones included.
    /// </summary>
    public JToken? GetContextSampleFor(BaseGraphTask task) => GetContextSampleFor(task, [], []);

    private JToken? GetContextSampleFor(BaseGraphTask task, Dictionary<Guid, JToken?> resolved, HashSet<Guid> visiting)
    {
        if (resolved.TryGetValue(task.Id, out JToken? cached))
            return cached;

        if (!visiting.Add(task.Id))
            return ScopeContext; // cycle guard

        JToken? context = ScopeContext;
        foreach (var pre in _graph.GetPrevious(task))
            context = MergeContexts(context, GetContextLeaving(pre, resolved, visiting));

        visiting.Remove(task.Id);
        resolved[task.Id] = context;
        return context;
    }

    /// <summary>
    /// Context handed over by a task to the ones following it : the one it reads, plus its own
    /// settings when it is a context setter.
    /// </summary>
    private JToken? GetContextLeaving(BaseGraphTask task, Dictionary<Guid, JToken?> resolved, HashSet<Guid> visiting)
    {
        JToken? context = GetContextSampleFor(task, resolved, visiting);
        if (task is not GraphControl control || !control.IsShare())
            return context;

        return ApplyContextSetter(context, GetContextSetterValues(control, context));
    }

    /// <summary>
    /// Values a context setter sets, its settings references being resolved against the context it
    /// reads and a sample of its previous outputs.
    /// </summary>
    private JToken? GetContextSetterValues(BaseGraphTask setter, JToken? context)
    {
        if (string.IsNullOrEmpty(setter.ParametersJson))
            return null;

        JToken? previous;
        if (setter.Settings.IsWaitingAllInputs)
        {
            JObject byName = new JObject();
            foreach (var sample in GetPreviousSamplesByName(setter))
                byName[sample.Key] = sample.Value;
            previous = byName;
        }
        else
        {
            previous = GetPreviousSamples(setter).FirstOrDefault();
        }

        JToken values = JToken.Parse(setter.ParametersJson);
        return ReferencesHandler.ReplaceReferences(values, GenerateContextFrom(previous, context)).ReplacedSetting;
    }

    /// <summary>
    /// Settings a context setter starts with : every key the context holds at that point of the
    /// graph, left unset, the user only filling the ones the branch overrides (and adding the keys
    /// specific to the workflow).
    /// </summary>
    public JObject GetContextSetterDefaultSettings(BaseGraphTask task)
    {
        JObject settings = new JObject();
        if (GetContextSampleFor(task) is JObject context)
            foreach (var property in context.Properties())
                settings[property.Name] = JValue.CreateNull();
        return settings;
    }

    /// <summary>
    /// Context a context setter hands over : [context] with the [values] it sets applied. A null
    /// value leaves the inherited one untouched (that is how an unset entry is stored), any other
    /// value overrides it and a key the context doesn't hold yet is added to it.
    /// </summary>
    public static JObject ApplyContextSetter(JToken? context, JToken? values)
    {
        JObject applied = context is JObject inherited ? (JObject)inherited.DeepClone() : new JObject();
        if (values is not JObject settings)
            return applied;

        foreach (var property in settings.Properties())
        {
            if (property.Value.Type == JTokenType.Null)
                continue;
            applied[property.Name] = property.Value.DeepClone();
        }

        return applied;
    }

    /// <summary>
    /// Context flowing into a task at runtime : the one carried by each of its previous instances
    /// merged together, falling back to [scopeContext] at the start of the graph.
    /// </summary>
    public static JToken? ResolveIncomingContext(IReadOnlyList<TaskInstance> previousInstances, JToken? scopeContext)
    {
        JToken? context = scopeContext;
        foreach (var previous in previousInstances)
            context = MergeContexts(context, previous.Context);
        return context;
    }

    /// <summary>
    /// Merge two contexts coming from two branches, the values of [other] winning. Anything that
    /// isn't an object can't be merged and is taken as-is.
    /// </summary>
    private static JToken? MergeContexts(JToken? context, JToken? other)
    {
        if (other == null)
            return context;
        if (context is not JObject source || other is not JObject values)
            return other;

        JObject merged = (JObject)source.DeepClone();
        merged.Merge(values, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Ignore
        });
        return merged;
    }

    #endregion

    /// <summary>
    /// Build the context for a task from its previous instances.
    /// If the task waits for all inputs the context is keyed by previous node name,
    /// otherwise the single previous output is used as-is.
    /// Pass-through predecessors are transparently walked over: their output is skipped
    /// and the context reads from the nearest non-pass-through upstream instance(s).
    /// </summary>
    public JObject GetContextFor(BaseGraphTask task, IReadOnlyList<TaskInstance> previousInstances, JToken? context)
    {
        if (previousInstances.Count == 0 && context == null)
            return GenerateEmptyContext();

        if (task.Settings.IsWaitingAllInputs)
        {
            var byName = new Dictionary<string, JToken?>();
            foreach (var instance in previousInstances)
                foreach (var effective in ResolveEffectivePreviousInstances(instance))
                    byName[effective.NodeName] = effective.Output;
            return GenerateContextFrom(byName, context);
        }

        // Keep the single-output shape: take the first resolved ancestor of the first
        // previous instance. Pass-through never changes the consumer's context shape.
        TaskInstance? single = previousInstances.Count > 0
            ? ResolveEffectivePreviousInstances(previousInstances[0]).FirstOrDefault()
            : null;
        return GenerateContextFrom(single?.Output, context);
    }

    /// <summary>
    /// Walk back through pass-through task instances to the nearest non-pass-through
    /// ancestor(s). Mirrors <see cref="ResolveEffectivePreviousTasks"/> but at runtime.
    /// </summary>
    private static IEnumerable<TaskInstance> ResolveEffectivePreviousInstances(TaskInstance instance, HashSet<Guid>? visited = null)
    {
        if (instance.Node?.AutomationTask?.Settings.IsPassingThrough != true)
        {
            yield return instance;
            yield break;
        }

        visited ??= [];
        if (!visited.Add(instance.Id))
            yield break; // cycle guard

        if (instance.Previous.Count == 0)
        {
            yield return instance;
            yield break;
        }

        foreach (var prev in instance.Previous)
            foreach (var resolved in ResolveEffectivePreviousInstances(prev, visited))
                yield return resolved;
    }

    public JObject GenerateEmptyContext()
    {
        return new JObject
        {
            [PreviousIdentifier] = new JObject(),
            [SharedIdentifier] = new JObject()
        };
    }

    public JObject GenerateContextFrom(JToken? previous, JToken? context)
    {
        return new JObject
        {
            [PreviousIdentifier] = previous,
            [SharedIdentifier] = context,
        };
    }

    public JObject GenerateContextFrom(Dictionary<string, JToken?> previous, JToken? context)
    {
        JObject ctxt = GenerateEmptyContext();

        ctxt[SharedIdentifier] = context;
        foreach (var pre in previous)
        {
            JToken previousContext = ctxt[PreviousIdentifier] ?? new JObject();
            previousContext[pre.Key] = pre.Value;
            ctxt[PreviousIdentifier] ??= previousContext;
        }

        return ctxt;
    }
}
