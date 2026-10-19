using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Annotations;

namespace Fantoche.Shared.Tests;

/// <summary>
/// A "$row" reference is left to the task, but only in the properties its schema marks as evaluated
/// per row : anywhere else it is an error, the way a reference to nothing is.
/// </summary>
[TestFixture]
public class RowScopeReferencesTests
{
    private class Parameters
    {
        [JsonSchemaExtensionData(RowScope.SchemaKey, RowScope.Name)]
        public List<Condition> Conditions { get; set; } = [];

        public object? Other { get; set; }
    }

    private class Condition
    {
        public object? Left { get; set; }
    }

    private static readonly JObject Context = new() { ["previous"] = new JObject { ["Value"] = 1 } };

    private static ReferenceReplaceResult Replace(object template, JsonSchema? schema)
        => ReferencesHandler.ReplaceReferences(JToken.FromObject(template), Context, schema);

    [Test]
    public void In_a_marked_property_the_reference_is_left_for_the_task()
    {
        var template = new { Conditions = new[] { new { Left = "$row.columns.amount" } } };

        ReferenceReplaceResult result = Replace(template, JsonSchema.FromType<Parameters>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Replaced.SelectToken("Conditions[0].Left")?.Value<string>(), Is.EqualTo("$row.columns.amount"));
        });
    }

    [Test]
    public void Anywhere_else_the_reference_is_an_error()
    {
        var template = new { Other = "$row.index" };

        ReferenceReplaceResult result = Replace(template, JsonSchema.FromType<Parameters>());

        Assert.That(result.Errors, Is.EqualTo(new[] { "[$row.index] is only available in a field evaluated per row." }));
    }

    [Test]
    public void Without_a_schema_the_reference_is_an_error()
    {
        ReferenceReplaceResult result = Replace(new { Conditions = "$row.index" }, null);

        Assert.That(result.HasErrors, Is.True);
    }

    [Test]
    public void The_mark_survives_the_schema_being_stored_as_text()
    {
        JsonSchema schema = Schemas.Parse(JsonSchema.FromType<Parameters>().ToJson())!;
        var template = new { Conditions = new[] { new { Left = "$row.index" } } };

        Assert.That(Replace(template, schema).Errors, Is.Empty);
    }

    [Test]
    public void The_other_references_of_a_marked_property_are_still_resolved()
    {
        var template = new { Conditions = new[] { new { Left = "$previous.Value" } } };

        ReferenceReplaceResult result = Replace(template, JsonSchema.FromType<Parameters>());

        Assert.That(result.Replaced.SelectToken("Conditions[0].Left")?.Value<int>(), Is.EqualTo(1));
    }
}
