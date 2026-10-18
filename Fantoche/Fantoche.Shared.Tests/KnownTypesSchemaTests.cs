using Fantoche.Plugins.Shared;
using NJsonSchema;

namespace Fantoche.Shared.Tests;

/// <summary>
/// The storage handles are tagged in the schema of a task, which is how an editor tells a table
/// apart from any other object.
/// </summary>
[TestFixture]
public class KnownTypesSchemaTests
{
    private class Parameters
    {
        public TableRef Source { get; set; } = new();
        public FileRef Template { get; set; } = new();
    }

    [Test]
    public void A_handle_property_is_tagged_with_its_known_type()
    {
        JsonSchema schema = JsonSchema.FromType<Parameters>();

        Assert.Multiple(() =>
        {
            Assert.That(TypeOf(schema, nameof(Parameters.Source)), Is.EqualTo(KnownTypes.Table));
            Assert.That(TypeOf(schema, nameof(Parameters.Template)), Is.EqualTo(KnownTypes.File));
        });
    }

    [Test]
    public void The_tag_survives_the_schema_being_stored_as_text()
    {
        JsonSchema schema = Data.Schemas.Parse(JsonSchema.FromType<Parameters>().ToJson())!;

        Assert.That(TypeOf(schema, nameof(Parameters.Source)), Is.EqualTo(KnownTypes.Table));
    }

    private static object? TypeOf(JsonSchema schema, string property)
        => schema.Properties[property].ActualSchema.ExtensionData is { } data && data.TryGetValue(KnownTypes.SchemaKey, out object? type)
            ? type
            : null;
}
