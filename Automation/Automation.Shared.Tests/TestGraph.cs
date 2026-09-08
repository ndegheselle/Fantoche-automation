using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;

namespace Automation.Shared.Tests;

/// <summary>
/// A graph built for a test, its nodes reachable by name. Names are what the mappings of a node
/// merging several branches point at (see <c>GraphContextResolution.GetMultipleContextFor</c>), so
/// they are the same thing the assertions read.
/// </summary>
internal sealed class TestGraph
{
    private readonly Dictionary<string, BaseGraphTask> _nodes = [];
    private readonly Dictionary<Guid, BaseAutomationTask> _tasks = [];

    public TasksGraph Graph { get; } = new();

    public BaseGraphTask this[string name] => _nodes[name];

    /// <summary>
    /// A schema of an object holding [properties] as strings, so that two nodes declaring different
    /// ones hand over samples a mapping can tell apart.
    /// </summary>
    public static string ObjectSchema(params string[] properties)
    {
        IEnumerable<string> declarations = properties.Select(x => $"\"{x}\":{{\"type\":\"string\"}}");
        return $"{{\"type\":\"object\",\"properties\":{{{string.Join(',', declarations)}}}}}";
    }

    /// <summary>A schema of an object whose "data" property is itself an object holding a "url".</summary>
    public static string NestedDataSchema =>
        """{"type":"object","properties":{"data":{"type":"object","properties":{"url":{"type":"string"}}}}}""";

    /// <summary>A schema of an object whose "data" property is a string.</summary>
    public static string FlatDataSchema =>
        """{"type":"object","properties":{"data":{"type":"string"}}}""";

    public TestGraph Start(string mapping = "{}") => Control(AutomationControl.StartTask, "Start", mapping);
    public TestGraph End(string mapping = "{}") => Control(AutomationControl.EndTask, "End", mapping);
    public TestGraph Share(string name, string mapping) => Control(AutomationControl.ShareTask, name, mapping);
    public TestGraph Map(string name, string mapping) => Control(AutomationControl.MapTask, name, mapping);
    public TestGraph Join(string name, string mapping) => Control(AutomationControl.JoinTask, name, mapping);

    public TestGraph Control(AutomationControl control, string name, string mapping)
    {
        GraphControl node = new(control) { InputTemplateJson = mapping };
        node.Metadata.Name = name;
        return Add(name, node);
    }

    /// <summary>
    /// A task node named [name], mapping [mapping] and handing over a sample of [outputSchema].
    /// </summary>
    public TestGraph Task(string name, string mapping, string outputSchema = "{}")
    {
        AutomationTask task = new(name, Guid.Empty)
        {
            Id = Guid.NewGuid(),
            InputSchemaJson = "{}",
            OutputSchemaJson = outputSchema,
        };
        _tasks[task.Id] = task;

        return Add(name, new GraphTask(task) { InputTemplateJson = mapping });
    }

    public TestGraph Connect(string from, string to)
    {
        Graph.Connect(_nodes[from], _nodes[to]);
        return this;
    }

    /// <summary>
    /// The edge from [from] to [to] : a node holds a single connector of each kind, so the pair of
    /// them is the connection between the two.
    /// </summary>
    public GraphEdge Edge(string from, string to)
        => new(_nodes[from].Outputs[0].Id, _nodes[to].Inputs[0].Id);

    /// <summary>[edge] as the names of the two nodes it links, e.g. "Start-&gt;A".</summary>
    public string NameOf(GraphEdge edge)
    {
        var (from, to) = (_nodes.Values.First(x => x.Outputs.Any(c => c.Id == edge.SourceId)),
                          _nodes.Values.First(x => x.Inputs.Any(c => c.Id == edge.TargetId)));
        return $"{from.Name}->{to.Name}";
    }

    /// <summary>Refresh the graph and preview it.</summary>
    public PreviewResult Preview()
    {
        Graph.Refresh(_tasks);

        GraphContextResolution resolution = new(Guid.NewGuid());
        GraphExecutionPreview preview = new();
        preview.BuildSamples(Graph, resolution);

        return new PreviewResult(this, preview, resolution);
    }

    private TestGraph Add(string name, BaseGraphTask node)
    {
        _nodes.Add(name, node);
        Graph.Nodes.Add(node);
        return this;
    }
}

/// <summary>
/// What previewing a <see cref="TestGraph"/> came to, read by node name rather than by node id.
/// </summary>
internal sealed record PreviewResult(TestGraph Source, GraphExecutionPreview Preview, GraphContextResolution Resolution)
{
    /// <summary>The instances the node resolved with, one per context it was reached by.</summary>
    public IReadOnlyList<TaskInstance> Resolved(string name)
    {
        if (!Resolution.NodeInstances.TryGetValue(Source[name].Id, out List<TaskInstance>? instances))
            return [];
        return [.. instances.Where(x => x.State == EnumTaskState.Completed)];
    }

    /// <summary>The ways the node can be reached, as the preview found them.</summary>
    public IReadOnlyList<NodePreviewContext> Contexts(string name)
        => Preview.NodesContexts.TryGetValue(Source[name].Id, out List<NodePreviewContext>? contexts) ? contexts : [];

    public IReadOnlyList<GraphPreviewError> Errors(string name)
        => Preview.NodesErrors.TryGetValue(Source[name].Id, out List<GraphPreviewError>? errors) ? errors : [];

    public IReadOnlyList<GraphPreviewError> EdgeErrors(string from, string to)
        => Preview.EdgesErrors.TryGetValue(Source.Edge(from, to), out List<GraphPreviewError>? errors) ? errors : [];

    /// <summary>Every message held against any node of the graph.</summary>
    public IEnumerable<string> AllMessages => Preview.NodesErrors.SelectMany(x => x.Value).Select(x => x.Message);

    /// <summary>The path [error] was reached by, as node names.</summary>
    public IEnumerable<string> Path(GraphPreviewError error) => error.Provenance.Select(Source.NameOf);

    /// <summary>The edge [error] blames, as node names, null when it blames none.</summary>
    public string? Divergence(GraphPreviewError error)
        => error.DivergenceEdge.HasValue ? Source.NameOf(error.DivergenceEdge.Value) : null;
}
