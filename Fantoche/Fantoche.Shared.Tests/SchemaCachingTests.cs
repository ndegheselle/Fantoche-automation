using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using NJsonSchema;

namespace Fantoche.Shared.Tests;

/// <summary>
/// A parsed schema is kept rather than parsed on every read, parsing one being most of what walking
/// a graph costs. What that has to never do is hand back a schema the text no longer says.
/// </summary>
[TestFixture]
public class SchemaCachingTests
{
    [Test]
    public void A_schema_is_parsed_again_once_its_text_changed()
    {
        GraphTask node = new() { OutputSchemaJson = TestGraph.ObjectSchema("before") };
        Assert.That(node.OutputSchema!.Properties.Keys, Is.EquivalentTo(new[] { "before" }));

        node.OutputSchemaJson = TestGraph.ObjectSchema("after");
        Assert.That(node.OutputSchema!.Properties.Keys, Is.EquivalentTo(new[] { "after" }));
    }

    [Test]
    public void Clearing_the_text_clears_the_schema()
    {
        GraphTask node = new() { OutputSchemaJson = TestGraph.ObjectSchema("field") };
        _ = node.OutputSchema;

        node.OutputSchemaJson = null;

        Assert.That(node.OutputSchema, Is.Null);
    }

    [Test]
    public void Assigning_a_schema_writes_its_text_and_keeps_it()
    {
        JsonSchema schema = JsonSchema.FromJsonAsync(TestGraph.ObjectSchema("field")).Result;
        GraphTask node = new() { OutputSchema = schema };

        Assert.Multiple(() =>
        {
            Assert.That(node.OutputSchema, Is.SameAs(schema));
            Assert.That(node.OutputSchemaJson, Is.Not.Null);
        });
    }
}
