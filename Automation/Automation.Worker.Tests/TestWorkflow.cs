using Automation.Plugins;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Tests;

/// <summary>
/// A workflow built for a test, its nodes reachable by name. The names are what a mapping merging
/// several branches points at (<c>$previous.&lt;node&gt;.&lt;field&gt;</c>), so they are the same
/// thing the assertions read.
/// </summary>
internal sealed class TestWorkflow
{
    private readonly Dictionary<string, BaseGraphTask> _nodes = [];
    private readonly Dictionary<Guid, BaseAutomationTask> _tasks = [];
    private readonly List<TestWorkflow> _nested = [];

    public AutomationWorkflow Workflow { get; }

    public BaseGraphTask this[string name] => _nodes[name];

    /// <param name="requireOutput">
    /// Whether the workflow declares an output schema : a workflow that declares one and ends with
    /// nothing is an error, one that declares none simply ends empty (see <c>EndAsync</c>).
    /// </param>
    public TestWorkflow(string name = "Test", bool requireOutput = true)
    {
        Workflow = new AutomationWorkflow()
        {
            Id = Guid.NewGuid(),
            Metadata = new ScopedMetadata() { Name = name },
            InputSchema = new JsonSchema(),
            OutputSchema = requireOutput ? JsonSchema.FromType<TestResult>() : null,
        };
    }

    #region Nodes

    public TestWorkflow Start(object? mapping = null) => Control(AutomationControl.StartTask, "Start", mapping);
    public TestWorkflow End(object? mapping = null, string name = "End") => Control(AutomationControl.EndTask, name, mapping);
    public TestWorkflow Share(string name, object mapping) => Control(AutomationControl.ShareTask, name, mapping);
    public TestWorkflow Map(string name, object mapping) => Control(AutomationControl.MapTask, name, mapping);
    public TestWorkflow Join(string name, object mapping) => Control(AutomationControl.JoinTask, name, mapping);

    public TestWorkflow Control(AutomationControl control, string name, object? mapping)
    {
        GraphControl node = new(control) { InputTemplateJson = Mapping(mapping) };
        node.Metadata.Name = name;
        return Add(name, node);
    }

    /// <summary>
    /// A node named [name] running [task], with [mapping] as its input template : an object whose
    /// string values may reference the context (<c>$previous.X</c>, <c>$shared.Y</c>), or the raw
    /// template as a string when the test needs one that isn't valid JSON.
    /// </summary>
    public TestWorkflow Task(string name, AutomationTask task, object? mapping = null)
    {
        _tasks[task.Id] = task;
        // A node holds a copy of the metadata of the task it points at : the name of the node is
        // its own, and what a mapping merging several branches reads it by.
        GraphTask node = new(task) { InputTemplateJson = Mapping(mapping) };
        node.Metadata.Name = name;
        return Add(name, node);
    }

    /// <summary>A node running the whole workflow [nested], as a task of this one.</summary>
    public TestWorkflow Nested(string name, TestWorkflow nested, object? mapping = null)
    {
        AutomationWorkflow workflow = nested.Workflow;
        _nested.Add(nested);
        _tasks[workflow.Id] = workflow;
        GraphWorkflow node = new(workflow) { InputTemplateJson = Mapping(mapping) };
        node.Metadata.Name = name;
        return Add(name, node);
    }

    #endregion

    #region Connections

    public TestWorkflow Connect(string from, string to)
    {
        Workflow.Graph.Connect(_nodes[from], _nodes[to]);
        return this;
    }

    /// <summary>Connect every node of [names] to the next one.</summary>
    public TestWorkflow Chain(params string[] names)
    {
        for (int i = 0; i < names.Length - 1; i++)
            Connect(names[i], names[i + 1]);
        return this;
    }

    #endregion

    /// <summary>The workflow with its graph refreshed, ready to be executed.</summary>
    public AutomationWorkflow Refreshed()
    {
        foreach (TestWorkflow nested in _nested)
            nested.Refreshed();
        Workflow.Graph.Refresh(Tasks(), force: true);
        return Workflow;
    }

    /// <summary>An instance of the workflow started with [parameters].</summary>
    public WorkflowInstance Instance(object? parameters = null)
        => new(Refreshed())
        {
            Parameters = parameters == null ? null : JToken.FromObject(parameters)
        };

    /// <summary>Run the workflow with [parameters], see <see cref="WorkflowRun"/>.</summary>
    public Task<WorkflowRun> RunAsync(object? parameters = null, CancellationToken? cancellation = null)
        => WorkflowRun.ExecuteAsync(Instance(parameters), cancellation);

    /// <summary>Run the workflow with [parameters], handing back what it failed with if it did.</summary>
    public Task<(WorkflowRun Run, Exception? Error)> TryRunAsync(object? parameters = null, CancellationToken? cancellation = null)
        => WorkflowRun.TryExecuteAsync(Instance(parameters), cancellation);

    /// <summary>
    /// Every task the graph points at : what <see cref="TasksGraph.Refresh"/> needs to load the
    /// nodes. The controls are known by the graph on its own, they are only here so a graph can be
    /// walked with a single dictionary.
    /// </summary>
    private Dictionary<Guid, BaseAutomationTask> Tasks()
    {
        Dictionary<Guid, BaseAutomationTask> tasks = new(_tasks);
        foreach (AutomationControl control in AutomationControl.All)
            tasks[control.Id] = control;
        return tasks;
    }

    private static string? Mapping(object? mapping) => mapping switch
    {
        null => "{}",
        string raw => raw,
        _ => JsonConvert.SerializeObject(mapping)
    };

    private TestWorkflow Add(string name, BaseGraphTask node)
    {
        _nodes.Add(name, node);
        Workflow.Graph.Nodes.Add(node);
        return this;
    }
}
