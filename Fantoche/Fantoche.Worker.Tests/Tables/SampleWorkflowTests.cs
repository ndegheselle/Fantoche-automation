using Fantoche.Plugins.Example.Tasks.Tables;
using Fantoche.Plugins.Shared;
using Fantoche.Services.Local;
using Fantoche.Services.Local.Database;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Samples;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The samples created when the example package is added (see <see cref="ExampleSamples"/>), as the
/// editor shows them : the schemas come from the classes of the package, the mappings of the
/// workflow are written by hand, and nothing but this checks they fit.
/// </summary>
[TestFixture]
internal sealed class SampleWorkflowTests
{
    private string _folder = null!;
    private LocalScopedService _scoped = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _scoped = new LocalScopedService(new DatabaseFactory(Path.Combine(_folder, "fantoche.db")));
    }

    [TearDown]
    public void TearDown()
    {
        // SQLite keeps the file open in its pool until told otherwise.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, true);
    }

    /// <summary>The task classes of the example package, as the supervisor lists them.</summary>
    private static List<ClassTarget> ExampleClasses()
        => [.. typeof(ImportCsvTask).Assembly.GetTypes()
            .Where(type => typeof(ITask).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false })
            .Select(type => new ClassTarget { ClassFullName = type.FullName!, Dll = ExampleSamples.PackageId })];

    /// <summary>The class [target] points at, as a worker loads it.</summary>
    private static Task<ITask> LoadAsync(PackageClassTarget target)
    {
        Type type = typeof(ImportCsvTask).Assembly.GetType(target.ClassFullName)!;
        return Task.FromResult((ITask)Activator.CreateInstance(type)!);
    }

    private Task<SamplesCreated> CreateAsync() => ExampleSamples.CreateAsync(_scoped, ExampleClasses(), LoadAsync);

    /// <summary>The workflow of the samples, and what the editor previews of it.</summary>
    private async Task<(AutomationWorkflow Workflow, GraphExecutionPreview Preview)> LoadWorkflowAsync()
    {
        List<ScopedElement> tree = await _scoped.GetTreeAsync();
        Guid id = tree.OfType<AutomationWorkflow>().Single(x => x.Metadata.Name == ExampleSamples.WorkflowName).Id;
        AutomationWorkflow workflow = (AutomationWorkflow)await _scoped.LoadRunnableAsync(id);
        GraphExecutionPreview preview = new();
        preview.BuildSamples(workflow.Graph, new GraphContextResolution());
        return (workflow, preview);
    }

    [Test]
    public async Task Workflow_ResolvesEveryReference()
    {
        await CreateAsync();
        (_, GraphExecutionPreview preview) = await LoadWorkflowAsync();

        IEnumerable<string> errors = preview.NodesErrors.SelectMany(x => x.Value).Select(x => x.Message);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task Workflow_FitsTheSchemasOfItsTasks()
    {
        await CreateAsync();
        (AutomationWorkflow workflow, GraphExecutionPreview preview) = await LoadWorkflowAsync();

        List<string> errors = [];
        // Asking the user has no mapping of its own : it is handed the parameters of its task.
        foreach (GraphTask node in workflow.Graph.Nodes.OfType<GraphTask>().Where(x => x is not GraphUserInterface))
        {
            List<NodePreviewContext> contexts = preview.NodesContexts.GetValueOrDefault(node.Id) ?? [];
            JToken mapping = JToken.Parse(node.InputTemplateJson!);
            errors.AddRange(MappingCheck.Check(mapping, contexts, node.InputSchema, allowMissingRequired: false)
                .Select(error => $"{node.Name} : {error.Message}"));
        }

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task Merge_ReadsTheNamesTheMappingsGive()
    {
        await CreateAsync();
        (AutomationWorkflow workflow, GraphExecutionPreview preview) = await LoadWorkflowAsync();
        BaseGraphTask merge = workflow.Graph.Nodes.OfType<BaseGraphTask>().Single(x => x.Name == "Merge");

        JObject context = preview.NodesContexts[merge.Id].Single().Context;

        JToken? mapping = context.SelectToken("previous.Orders.Mapping");
        Assert.That(mapping?.ToObject<Dictionary<string, string>>()?.Keys, Is.EqualTo(new[] { "OrderId", "CustomerId", "Amount", "Date" }));
    }

    [Test]
    public async Task Tasks_FollowTheNewestVersionOfThePackage()
    {
        await CreateAsync();

        List<AutomationTask> tasks = [.. (await _scoped.GetTreeAsync()).OfType<AutomationTask>().Where(x => x.Target?.Package.Id == ExampleSamples.PackageId)];

        Assert.Multiple(() =>
        {
            Assert.That(tasks, Is.Not.Empty);
            Assert.That(tasks.Select(x => x.Target!.Package.Version), Is.All.Null);
        });
    }

    [Test]
    public async Task PackageAddedAgain_CreatesNothingTwice()
    {
        await CreateAsync();

        SamplesCreated again = await CreateAsync();

        List<ScopedElement> tree = await _scoped.GetTreeAsync();
        Assert.Multiple(() =>
        {
            Assert.That(again.Elements, Is.Empty);
            Assert.That(tree.Count(x => x.Metadata.Name == ExampleSamples.WorkflowName), Is.EqualTo(1));
            Assert.That(tree.Count(x => x.Metadata.Name == "Map columns"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Interfaces_AreGivenToTheTasksTheyDeclare()
    {
        await CreateAsync();
        PackageClassTarget mapColumnsInterface = new(new PackageIdentifier { Id = ExampleSamples.InterfacesPackageId, Version = new Version(1, 0, 0) }, "Fantoche.Plugins.Example.Ui.Tables.MapColumnsInterface")
        {
            Dll = ExampleSamples.InterfacesPackageId,
        };

        int attached = await ExampleSamples.AttachInterfacesAsync(_scoped, [new TaskInterfaceClass(mapColumnsInterface, typeof(MapColumnsTask).FullName)]);

        List<ScopedElement> tree = await _scoped.GetTreeAsync();
        AutomationTask mapColumns = tree.OfType<AutomationTask>().Single(x => x.Target?.ClassFullName == typeof(MapColumnsTask).FullName);
        AutomationInterface face = tree.OfType<AutomationInterface>().Single(x => !x.IsForm);
        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.EqualTo(1));
            Assert.That(face.ParentId, Is.EqualTo(mapColumns.Id), "linked by hanging under the task");
            Assert.That(face.Target?.ClassFullName, Is.EqualTo(mapColumnsInterface.ClassFullName));
            Assert.That(face.Target?.Package.Version, Is.Null, "the newest version each time it is shown");
        });
    }

    [Test]
    public async Task TaskLoadedToRun_CarriesTheInterfaceLinkedToIt()
    {
        await CreateAsync();
        PackageClassTarget mapColumnsInterface = new(new PackageIdentifier { Id = ExampleSamples.InterfacesPackageId }, "Fantoche.Plugins.Example.Ui.Tables.MapColumnsInterface");
        await ExampleSamples.AttachInterfacesAsync(_scoped, [new TaskInterfaceClass(mapColumnsInterface, typeof(MapColumnsTask).FullName)]);

        (AutomationWorkflow workflow, _) = await LoadWorkflowAsync();

        // What a run asking the user hands the application, the task of the node.
        AutomationTask mapColumns = (AutomationTask)workflow.Graph.Nodes.OfType<GraphTask>().First(x => x.Name == "Customers").AutomationTask!;
        AutomationTask merge = (AutomationTask)workflow.Graph.Nodes.OfType<GraphTask>().First(x => x.Name == "Merge").AutomationTask!;
        Assert.Multiple(() =>
        {
            Assert.That(mapColumns.Interface?.ClassFullName, Is.EqualTo(mapColumnsInterface.ClassFullName));
            Assert.That(merge.Interface, Is.Null, "the generated form");
        });
    }

    [Test]
    public async Task Interfaces_AreLinkedOnceToATask()
    {
        await CreateAsync();
        PackageClassTarget mapColumnsInterface = new(new PackageIdentifier { Id = ExampleSamples.InterfacesPackageId }, "Fantoche.Plugins.Example.Ui.Tables.MapColumnsInterface");
        TaskInterfaceClass found = new(mapColumnsInterface, typeof(MapColumnsTask).FullName);
        await ExampleSamples.AttachInterfacesAsync(_scoped, [found]);

        int again = await ExampleSamples.AttachInterfacesAsync(_scoped, [found]);

        Assert.That(again, Is.Zero);
    }

    [Test]
    public async Task FreshDatabase_HoldsTheGeneratedFormAmongTheControls()
    {
        ScopedElement? form = await _scoped.GetAsync(AutomationInterface.Form.Id);

        Assert.Multiple(() =>
        {
            Assert.That(form, Is.InstanceOf<AutomationInterface>());
            Assert.That(form?.ParentId, Is.EqualTo(Scope.Controls.Id));
        });
    }

    [TestCase("Fantoche.Plugins.Example.Tasks.Tables.ImportCsvTask", "Import csv")]
    [TestCase("Fantoche.Plugins.Example.Tasks.Tables.MapColumnsTask", "Map columns")]
    [TestCase("Some.Merge", "Merge")]
    public void Task_IsNamedAfterItsClass(string classFullName, string expected)
    {
        Assert.That(ExampleSamples.NameOf(classFullName), Is.EqualTo(expected));
    }
}
