using Fantoche.Services.Local;
using Fantoche.Services.Local.Database;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The workflows a fresh install holds (see DatabaseSeeder), as the editor shows them : their schemas
/// are written by hand, so nothing but this checks that they fit the mappings of the samples.
/// </summary>
[TestFixture]
internal sealed class SampleWorkflowTests
{
    private static readonly Guid CustomersOrdersId = new("00000000-0000-0000-0000-200000000024");
    private static readonly Guid PauseStopResumeId = new("00000000-0000-0000-0000-200000000025");
    private static readonly Guid[] SampleIds = [CustomersOrdersId, PauseStopResumeId];

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

    /// <summary>The sample [id], and what the editor previews of it.</summary>
    private async Task<(AutomationWorkflow Workflow, GraphExecutionPreview Preview)> LoadAsync(Guid id)
    {
        AutomationWorkflow workflow = (AutomationWorkflow)await _scoped.LoadRunnableAsync(id);
        GraphExecutionPreview preview = new();
        preview.BuildSamples(workflow.Graph, new GraphContextResolution());
        return (workflow, preview);
    }

    [TestCaseSource(nameof(SampleIds))]
    public async Task Sample_ResolvesEveryReference(Guid id)
    {
        (_, GraphExecutionPreview preview) = await LoadAsync(id);

        IEnumerable<string> errors = preview.NodesErrors.SelectMany(x => x.Value).Select(x => x.Message);

        Assert.That(errors, Is.Empty);
    }

    [TestCaseSource(nameof(SampleIds))]
    public async Task Sample_FitsTheSchemasOfItsTasks(Guid id)
    {
        (AutomationWorkflow workflow, GraphExecutionPreview preview) = await LoadAsync(id);

        List<string> errors = [];
        // A page has no mapping of its own : it is handed the parameters of its task.
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
        (AutomationWorkflow workflow, GraphExecutionPreview preview) = await LoadAsync(CustomersOrdersId);
        BaseGraphTask merge = workflow.Graph.Nodes.OfType<BaseGraphTask>().Single(x => x.Name == "Merge");

        JObject context = preview.NodesContexts[merge.Id].Single().Context;

        JToken? mapping = context.SelectToken("previous.Orders.Mapping");
        Assert.That(mapping?.ToObject<Dictionary<string, string>>()?.Keys, Is.EqualTo(new[] { "OrderId", "CustomerId", "Amount", "Date" }));
    }
}
