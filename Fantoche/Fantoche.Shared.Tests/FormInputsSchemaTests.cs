using Fantoche.Plugins.Shared;
using NJsonSchema;

namespace Fantoche.Shared.Tests;

/// <summary>
/// How a string of the parameters is typed is written in the schema of the task, the only thing the
/// form of the editor and of a run reads : a path is a string like any other otherwise.
/// </summary>
[TestFixture]
public class FormInputsSchemaTests
{
    private class Parameters
    {
        [FilePath("CSV files|*.csv")]
        public string Path { get; set; } = "";

        [Multiline]
        public string Notes { get; set; } = "";
    }

    [Test]
    public void A_file_path_is_written_with_its_filter()
    {
        JsonSchema property = PropertyOf(nameof(Parameters.Path));

        Assert.Multiple(() =>
        {
            Assert.That(property.ExtensionData?[FormInputs.SchemaKey], Is.EqualTo(FormInputs.File));
            Assert.That(property.ExtensionData?[FormInputs.FileFilterSchemaKey], Is.EqualTo("CSV files|*.csv"));
        });
    }

    [Test]
    public void The_input_survives_the_schema_being_stored_as_text()
    {
        JsonSchema property = PropertyOf(nameof(Parameters.Notes));

        Assert.That(property.ExtensionData?[FormInputs.SchemaKey], Is.EqualTo(FormInputs.Multiline));
    }

    /// <summary>The schema of [name] as the editor reads it : generated, stored as text, parsed again.</summary>
    private static JsonSchema PropertyOf(string name)
    {
        string stored = Data.Schemas.Of(typeof(Parameters)).ToJson();
        JsonSchema schema = Data.Schemas.Parse(stored)!;
        return schema.Properties[name];
    }
}
