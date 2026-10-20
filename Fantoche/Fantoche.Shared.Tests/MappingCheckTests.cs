using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Shared.Tests;

[TestFixture]
public class MappingCheckTests
{
    private static readonly JsonSchema RequiresName = JsonSchema.FromJsonAsync("""
        {"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}
        """).Result;

    private static NodePreviewContext Reached(string previous, params string[] branches) => new()
    {
        Branches = branches,
        Context = JObject.Parse($$"""{"previous":{{previous}}}"""),
    };

    [Test]
    public void A_reference_missing_on_one_branch_is_reported_on_that_branch_only()
    {
        JObject mapping = JObject.Parse("""{"name":"$previous.Name"}""");
        NodePreviewContext[] contexts = [Reached("""{"Name":"a"}""", "A"), Reached("{}", "B")];

        List<MappingError> errors = MappingCheck.Check(mapping, contexts, null, allowMissingRequired: false);

        Assert.That(errors.Select(error => error.Branch), Is.EqualTo(new[] { "B" }));
    }

    [Test]
    public void The_branch_is_not_named_when_the_node_is_reached_one_way()
    {
        JObject mapping = JObject.Parse("""{"name":"$previous.Name"}""");
        NodePreviewContext[] contexts = [Reached("{}", "A")];

        List<MappingError> errors = MappingCheck.Check(mapping, contexts, null, allowMissingRequired: false);

        Assert.That(errors.Single().HasBranch, Is.False);
    }

    [Test]
    public void A_resolved_value_not_matching_the_expected_schema_is_reported()
    {
        JObject mapping = JObject.Parse("""{"name":"$previous.Count"}""");
        NodePreviewContext[] contexts = [Reached("""{"Count":3}""")];

        List<MappingError> errors = MappingCheck.Check(mapping, contexts, RequiresName, allowMissingRequired: false);

        Assert.That(errors, Has.Count.EqualTo(1));
    }

    [Test]
    public void An_unresolved_reference_is_not_also_checked_against_the_schema()
    {
        JsonSchema expectsCount = JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"count":{"type":"integer"}}}
            """).Result;
        // Left unresolved, "$previous.Count" is a string, which an integer field would reject.
        JObject mapping = JObject.Parse("""{"count":"$previous.Count"}""");
        NodePreviewContext[] contexts = [Reached("{}")];

        List<MappingError> errors = MappingCheck.Check(mapping, contexts, expectsCount, allowMissingRequired: false);

        Assert.That(errors.Single().Message, Does.Contain("not found"));
    }

    [Test]
    public void A_missing_required_property_is_allowed_when_asked()
    {
        NodePreviewContext[] contexts = [Reached("{}")];

        List<MappingError> allowed = MappingCheck.Check(new JObject(), contexts, RequiresName, allowMissingRequired: true);
        List<MappingError> refused = MappingCheck.Check(new JObject(), contexts, RequiresName, allowMissingRequired: false);

        Assert.That(allowed, Is.Empty);
        Assert.That(refused, Has.Count.EqualTo(1));
    }
}
