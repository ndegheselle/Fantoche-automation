using Fantoche.Services.Local;
using Fantoche.Services.Local.Database;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The workflow a fresh install holds (see DatabaseSeeder), as the editor shows it : its schemas are
/// written by hand, so nothing but this checks that they fit the mappings of the sample.
/// </summary>
[TestFixture]
internal sealed class SampleWorkflowTests
{
    private static readonly Guid CustomersOrdersId = new("00000000-0000-0000-0000-200000000024");

    private string _folder = null!;
    private GraphExecutionPreview _preview = null!;
    private AutomationWorkflow _workflow = null!;

    [SetUp]
    public async Task SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        LocalScopedService scoped = new(new DatabaseFactory(Path.Combine(_folder, "fantoche.db")));
        _workflow = (AutomationWorkflow)await scoped.LoadRunnableAsync(CustomersOrdersId);

        _preview = new GraphExecutionPreview();
        _preview.BuildSamples(_workflow.Graph, new GraphContextResolution());
    }

    [TearDown]
    public void TearDown()
    {
        // SQLite keeps the file open in its pool until told otherwise.
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, true);
    }

    private BaseGraphTask Node(string name) => _workflow.Graph.Nodes.OfType<BaseGraphTask>().Single(x => x.Name == name);

    [Test]
    public void Sample_ResolvesEveryReference()
    {
        IEnumerable<string> errors = _preview.NodesErrors.SelectMany(x => x.Value).Select(x => x.Message);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Sample_FitsTheSchemasOfItsTasks()
    {
        List<string> errors = [];
        foreach (GraphTask node in _workflow.Graph.Nodes.OfType<GraphTask>())
        {
            List<NodePreviewContext> contexts = _preview.NodesContexts.GetValueOrDefault(node.Id) ?? [];
            JToken mapping = JToken.Parse(node.InputTemplateJson!);
            errors.AddRange(MappingCheck.Check(mapping, contexts, node.InputSchema, allowMissingRequired: false)
                .Select(error => $"{node.Name} : {error.Message}"));
        }

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Merge_ReadsTheNamesTheMappingsGive()
    {
        JObject context = _preview.NodesContexts[Node("Merge").Id].Single().Context;

        JToken? mapping = context.SelectToken("previous.Orders.Mapping");
        Assert.That(mapping?.ToObject<Dictionary<string, string>>()?.Keys, Is.EqualTo(new[] { "OrderId", "CustomerId", "Amount", "Date" }));
    }
}
