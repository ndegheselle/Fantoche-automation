using Fantoche.Shared.Data;
using NJsonSchema;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;
using RequiredAttribute = System.ComponentModel.DataAnnotations.RequiredAttribute;

namespace Fantoche.Shared.Tests;

/// <summary>
/// What a task declares on its parameters reaches its schema, which is what the editor, the
/// schedules and the run all check and display.
/// </summary>
[TestFixture]
public class SchemaDescriptionTests
{
    public class Parameters
    {
        [Description("Rows per batch.")]
        [Range(1, 1000)]
        public int BatchSize { get; set; } = 10;

        /// <summary>Not for the editor.</summary>
        [Required]
        public string Name { get; set; } = "";
    }

    [Test]
    public void A_property_is_described_by_its_description_attribute()
    {
        JsonSchema schema = Schemas.Of(typeof(Parameters));

        Assert.That(schema.ActualProperties[nameof(Parameters.BatchSize)].Description, Is.EqualTo("Rows per batch."));
    }

    [Test]
    public void The_xml_documentation_of_a_property_is_not_its_description()
    {
        JsonSchema schema = Schemas.Of(typeof(Parameters));

        Assert.That(schema.ActualProperties[nameof(Parameters.Name)].Description, Is.Null);
    }

    [Test]
    public void The_limits_of_a_property_are_checked()
    {
        JsonSchema schema = Schemas.Of(typeof(Parameters));

        IEnumerable<string> errors = schema.Validate("""{"BatchSize":0,"Name":"a"}""").Select(Schemas.Describe);

        Assert.That(errors, Is.EqualTo(new[] { "BatchSize : at least 1" }));
    }

    [Test]
    public void A_missing_required_property_is_named()
    {
        JsonSchema schema = JsonSchema.FromJsonAsync("""
            {"type":"object","required":["Name"],"properties":{"Name":{"type":"string"}}}
            """).Result;

        IEnumerable<string> errors = schema.Validate("{}").Select(Schemas.Describe);

        Assert.That(errors, Is.EqualTo(new[] { "Name : required" }));
    }
}
