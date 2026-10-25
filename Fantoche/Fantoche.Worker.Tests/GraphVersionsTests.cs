using System.Text.Json;
using Dapper;
using Fantoche.Services.Local;
using Fantoche.Services.Remote;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Fantoche.Supervisor.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The versions of the graph of a workflow : each save changing it adds one, and a past one is
/// rebuilt as it was, while only what changed is kept of it. Run against both supervisors, the one
/// of the process and a remote one.
/// </summary>
[TestFixture(SupervisorExecutionTests.Mode.Local)]
[TestFixture(SupervisorExecutionTests.Mode.Remote)]
internal sealed class GraphVersionsTests
{
    private readonly SupervisorExecutionTests.Mode _mode;
    private string _folder = null!;
    private WebApplication? _api;
    private ISupervisor _supervisor = null!;
    private AutomationTask _test = null!;

    public GraphVersionsTests(SupervisorExecutionTests.Mode mode)
    {
        _mode = mode;
    }

    [SetUp]
    public async Task SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        string supervisorFolder = Path.Combine(_folder, "supervisor");

        if (_mode == SupervisorExecutionTests.Mode.Local)
        {
            _supervisor = new LocalSupervisor(supervisorFolder);
        }
        else
        {
            _api = SupervisorApp.Build([
                "--urls", "http://127.0.0.1:0",
                $"--DATA_FOLDER={supervisorFolder}",
                "--Logging:LogLevel:Default=Warning"]);
            await _api.StartAsync();
            _supervisor = new SupervisorClient(new Uri($"{_api.Urls.First()}/"));
        }

        _test = PluginTasks.Make("Fantoche.TestPlugins.TestTask", "Test", JsonSchema.FromType<TestPlugins.TestParameters>(), JsonSchema.FromType<TestPlugins.TestResult>());
        _test.ParentId = Scope.Root.Id;
        await _supervisor.Scoped.CreateAsync(_test);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_supervisor is IAsyncDisposable client)
            await client.DisposeAsync();
        if (_api != null)
            await _api.DisposeAsync();
    }

    /// <summary><c>Start ─> First(+10) ─> End</c></summary>
    private TestWorkflow BuildLine(string name = "Line") => new TestWorkflow(name)
        .Start()
        .Task("First", _test, new { Message = "first", Value = "$previous.Value", Add = 10 })
        .End(new { Value = "$previous.Value", Message = "$previous.Message" })
        .Chain("Start", "First", "End");

    /// <summary>[workflow] stored, version 1.</summary>
    private async Task<AutomationWorkflow> StoreAsync(TestWorkflow workflow)
    {
        workflow.Workflow.ParentId = Scope.Root.Id;
        await _supervisor.Scoped.CreateAsync(workflow.Workflow);
        return await ReadAsync(workflow.Workflow.Id);
    }

    /// <summary>The line stored, version 1.</summary>
    private Task<AutomationWorkflow> StoreLineAsync() => StoreAsync(BuildLine());

    private async Task<AutomationWorkflow> ReadAsync(Guid workflowId)
        => (AutomationWorkflow)(await _supervisor.Scoped.GetAsync(workflowId))!;

    private static BaseGraphTask Node(AutomationWorkflow workflow, string name)
        => workflow.Graph.Nodes.OfType<BaseGraphTask>().Single(x => x.Name == name);

    /// <summary>Version 2 : what First adds changed to 20.</summary>
    private async Task<AutomationWorkflow> ChangeFirstAsync(AutomationWorkflow workflow)
    {
        Node(workflow, "First").InputTemplateJson = """{ "Message": "first", "Value": "$previous.Value", "Add": 20 }""";
        await _supervisor.Scoped.EditAsync(workflow);
        return await ReadAsync(workflow.Id);
    }

    /// <summary>Version 3 : a node Second between First and the end.</summary>
    private async Task<AutomationWorkflow> InsertSecondAsync(AutomationWorkflow workflow)
    {
        GraphTask second = new(_test) { InputTemplateJson = """{ "Value": "$previous.Value", "Add": 100 }""" };
        second.Metadata.Name = "Second";
        workflow.Graph.Nodes.Add(second);
        GraphConnection firstToEnd = workflow.Graph.Connections.Single(x => Node(workflow, "First").Outputs.Any(output => output.Id == x.SourceId));
        workflow.Graph.Connections.Remove(firstToEnd);
        workflow.Graph.Refresh(force: true);
        workflow.Graph.Connect(Node(workflow, "First"), second);
        workflow.Graph.Connect(second, Node(workflow, "End"));
        await _supervisor.Scoped.EditAsync(workflow);
        return await ReadAsync(workflow.Id);
    }

    /// <summary>The connections of [workflow] by the names of the nodes they link.</summary>
    private static List<string> Links(AutomationWorkflow workflow)
    {
        Dictionary<Guid, string> nodeOfConnector = [];
        foreach (BaseGraphTask node in workflow.Graph.Nodes.OfType<BaseGraphTask>())
        {
            foreach (GraphConnector connector in node.Inputs.Concat(node.Outputs))
                nodeOfConnector[connector.Id] = node.Name;
        }
        return [.. workflow.Graph.Connections.Select(x => $"{nodeOfConnector[x.SourceId]} -> {nodeOfConnector[x.TargetId]}").Order()];
    }

    [Test]
    public async Task ChangedGraph_IsANewVersion()
    {
        AutomationWorkflow workflow = await StoreLineAsync();

        AutomationWorkflow changed = await ChangeFirstAsync(workflow);

        Assert.Multiple(() =>
        {
            Assert.That(workflow.Version, Is.EqualTo(2), "the saved workflow is told its version");
            Assert.That(changed.Version, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task SaveLeavingTheGraphAsItIs_AddsNoVersion()
    {
        AutomationWorkflow workflow = await StoreLineAsync();

        workflow.Metadata.Name = "Renamed";
        await _supervisor.Scoped.EditAsync(workflow);

        Assert.That((await ReadAsync(workflow.Id)).Version, Is.EqualTo(1));
    }

    [Test]
    public async Task PastVersion_IsRebuiltAsItWas()
    {
        AutomationWorkflow workflow = await StoreLineAsync();
        workflow = await ChangeFirstAsync(workflow);
        await InsertSecondAsync(workflow);

        AutomationWorkflow first = (await _supervisor.Scoped.GetVersionAsync(workflow.Id, 1))!;
        AutomationWorkflow second = (await _supervisor.Scoped.GetVersionAsync(workflow.Id, 2))!;

        Assert.Multiple(() =>
        {
            Assert.That(first.Version, Is.EqualTo(1));
            Assert.That(Node(first, "First").InputTemplateJson, Does.Contain("10"));
            Assert.That(Links(first), Is.EqualTo(new[] { "First -> End", "Start -> First" }));
            Assert.That(Node(second, "First").InputTemplateJson, Does.Contain("20"));
            Assert.That(second.Graph.Nodes.OfType<BaseGraphTask>().Select(x => x.Name), Does.Not.Contain("Second"));
        });
    }

    [Test]
    public async Task LatestVersion_HoldsEveryChange()
    {
        AutomationWorkflow workflow = await StoreLineAsync();
        workflow = await ChangeFirstAsync(workflow);
        AutomationWorkflow latest = await InsertSecondAsync(workflow);

        Assert.Multiple(() =>
        {
            Assert.That(latest.Version, Is.EqualTo(3));
            Assert.That(Links(latest), Is.EqualTo(new[] { "First -> Second", "Second -> End", "Start -> First" }));
        });
    }

    [Test]
    public async Task PastVersion_OnlyKeepsWhatChanged()
    {
        AutomationWorkflow workflow = await StoreLineAsync();

        await ChangeFirstAsync(workflow);

        // Read from the database itself : what a version costs is what it stores.
        using SqliteConnection connection = new($"Data Source={Path.Combine(_folder, "supervisor", "fantoche.db")}");
        string changes = await connection.QuerySingleAsync<string>(
            "SELECT Changes FROM GraphVersions WHERE WorkflowId = @id AND Version = 1;",
            new { id = workflow.Id });
        using JsonDocument stored = JsonDocument.Parse(changes);
        Assert.That(stored.RootElement.GetProperty("Nodes").EnumerateObject().Select(x => x.Name), Is.EqualTo(new[] { Node(workflow, "First").Id.ToString() }));
    }

    [Test]
    public async Task UnknownVersion_IsNothing()
    {
        AutomationWorkflow workflow = await StoreLineAsync();

        Assert.That(await _supervisor.Scoped.GetVersionAsync(workflow.Id, 5), Is.Null);
    }

    [Test]
    public async Task PastVersion_RunsWithTheTasksItPointsAt()
    {
        AutomationWorkflow workflow = await StoreLineAsync();
        // Version 2 runs straight from the start to the end : no node points at the task anymore.
        GraphConnector startOutput = Node(workflow, "Start").Outputs.Single();
        GraphConnector endInput = Node(workflow, "End").Inputs.Single();
        workflow.Graph.Nodes.Remove(Node(workflow, "First"));
        workflow.Graph.Connections.Clear();
        workflow.Graph.Connections.Add(new GraphConnection { SourceId = startOutput.Id, TargetId = endInput.Id });
        await _supervisor.Scoped.EditAsync(workflow);

        var past = (AutomationWorkflow)await ScopedServiceExtensions.LoadRunnableAsync(_supervisor.Scoped, workflow.Id, version: 1);

        Assert.That(Node(past, "First").AutomationTask?.Id, Is.EqualTo(_test.Id));
    }

    [Test]
    public async Task NestedWorkflow_IsReadAtTheVersionTheRunStartedOn()
    {
        TestWorkflow line = BuildLine("Inner line");
        AutomationWorkflow inner = await StoreAsync(line);
        AutomationWorkflow outer = await StoreAsync(new TestWorkflow("Outer")
            .Start()
            .Nested("Inner", line, new { Value = "$previous.Value" })
            .End(new { Value = "$previous.Value" })
            .Chain("Start", "Inner", "End"));
        // The versions a run of it keeps as it starts.
        var started = (AutomationWorkflow)await ScopedServiceExtensions.LoadRunnableAsync(_supervisor.Scoped, outer.Id);
        Dictionary<Guid, int> versions = ScopedServiceExtensions.NestedVersionsOf(started);

        await ChangeFirstAsync(inner);
        var resumed = (AutomationWorkflow)await ScopedServiceExtensions.LoadRunnableAsync(_supervisor.Scoped, started.Id, started.Version, versions);
        var latest = (AutomationWorkflow)await ScopedServiceExtensions.LoadRunnableAsync(_supervisor.Scoped, started.Id);

        Assert.Multiple(() =>
        {
            var resumedInner = (AutomationWorkflow)Node(resumed, "Inner").AutomationTask!;
            var latestInner = (AutomationWorkflow)Node(latest, "Inner").AutomationTask!;
            Assert.That(resumedInner.Version, Is.EqualTo(1));
            Assert.That(Node(resumedInner, "First").InputTemplateJson, Does.Contain("10"));
            Assert.That(latestInner.Version, Is.EqualTo(2));
        });
    }
}
