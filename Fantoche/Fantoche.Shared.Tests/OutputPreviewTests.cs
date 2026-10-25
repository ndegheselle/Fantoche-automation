using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Annotations;

namespace Fantoche.Shared.Tests;

/// <summary>
/// A task isn't run by a preview : it hands over the sample of its output, and what it declares
/// handing over of its input (see <see cref="OutputPreview"/>). That is what lets an editor check
/// and offer the columns a mapping named, which the sample of a table knows nothing of.
/// <code>
/// Start ─> Import ─> Map ─> Read
/// </code>
/// </summary>
[TestFixture]
public class OutputPreviewTests
{
    private class MapParameters
    {
        [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]
        public TableRef Source { get; set; } = new();

        [JsonSchemaExtensionData(OutputPreview.MappingSchemaKey, OutputPreview.Whole)]
        public List<ColumnMapping> Columns { get; set; } = [];
    }

    /// <summary>The same parameters, declaring nothing.</summary>
    private class UndeclaredParameters
    {
        public TableRef Source { get; set; } = new();
        public List<ColumnMapping> Columns { get; set; } = [];
    }

    private class MergeParameters
    {
        [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]
        public TableRef Left { get; set; } = new();

        [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]
        public TableRef Right { get; set; } = new();
    }

    private static readonly string TableSchema = JsonSchema.FromType<TableRef>().ToJson();

    private const string MapMapping = """
        {"Source":"$previous","Columns":[{"Name":"CustomerId","Column":"client id"},{"Name":"City","Column":"town"}]}
        """;

    private static TestGraph Mapped(string inputSchema, string readMapping) => new TestGraph()
        .Start()
        .Task("Import", "{}", TableSchema)
        .Task("Map", MapMapping, TableSchema, inputSchema)
        .Task("Read", readMapping)
        .Connect("Start", "Import")
        .Connect("Import", "Map")
        .Connect("Map", "Read");

    [Test]
    public void The_node_after_a_mapping_reads_the_columns_it_names()
    {
        PreviewResult result = Mapped(JsonSchema.FromType<MapParameters>().ToJson(), """{"Key":"$previous.Mapping.CustomerId"}""").Preview();

        Assert.Multiple(() =>
        {
            Assert.That(result.Errors("Read"), Is.Empty);
            Assert.That(result.Resolved("Read").Single().Parameters?["Key"]?.Value<string>(), Is.EqualTo("client id"));
        });
    }

    [Test]
    public void A_task_declaring_nothing_hands_over_the_sample_of_its_output()
    {
        PreviewResult result = Mapped(JsonSchema.FromType<UndeclaredParameters>().ToJson(), """{"Key":"$previous.Mapping.CustomerId"}""").Preview();

        Assert.That(result.Errors("Read").Select(x => x.Message), Is.EqualTo(new[] { "[previous.Mapping.CustomerId] not found in context." }));
    }

    [Test]
    public void Tables_handed_over_to_the_same_place_merge_their_mappings()
    {
        string merge = JsonSchema.FromType<MergeParameters>().ToJson();
        PreviewResult result = new TestGraph()
            .Start()
            .Task("Left", "{}", TableSchema)
            .Task("Right", "{}", TableSchema)
            .Map("Tables", """{"Left":{"Id":"l","Mapping":{"CustomerId":"id"}},"Right":{"Id":"r","Mapping":{"Amount":"total"}}}""")
            .Task("Merge", """{"Left":"$previous.Left","Right":"$previous.Right"}""", TableSchema, merge)
            .Task("Read", """{"Customer":"$previous.Mapping.CustomerId","Amount":"$previous.Mapping.Amount"}""")
            .Connect("Start", "Tables")
            .Connect("Tables", "Merge")
            .Connect("Merge", "Read")
            .Preview();

        JToken? read = result.Resolved("Read").Single().Parameters;
        Assert.Multiple(() =>
        {
            Assert.That(read?["Customer"]?.Value<string>(), Is.EqualTo("id"));
            Assert.That(read?["Amount"]?.Value<string>(), Is.EqualTo("total"));
        });
    }
}
