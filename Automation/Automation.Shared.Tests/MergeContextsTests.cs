using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Shared.Tests;

[TestFixture]
public class MergeContextsTests
{
    [Test]
    public void The_values_of_the_other_context_win()
    {
        JToken? merged = GraphContextResolution.MergeContexts(
            JObject.Parse("""{"a":1,"b":1}"""),
            JObject.Parse("""{"b":2}"""));

        Assert.That(JToken.DeepEquals(merged, JObject.Parse("""{"a":1,"b":2}""")), Is.True);
    }

    [Test]
    public void A_missing_context_leaves_the_other_one_alone()
    {
        JObject context = JObject.Parse("""{"a":1}""");

        Assert.That(JToken.DeepEquals(GraphContextResolution.MergeContexts(context, null), context), Is.True);
    }

    [Test]
    public void A_json_null_leaves_the_context_alone()
    {
        JObject context = JObject.Parse("""{"a":1}""");

        // A null token stands for nothing to merge, not for a value overriding what is there.
        Assert.That(
            JToken.DeepEquals(GraphContextResolution.MergeContexts(context, JValue.CreateNull()), context),
            Is.True);
    }

    [Test]
    public void Anything_that_is_not_an_object_is_taken_as_is()
    {
        JToken? merged = GraphContextResolution.MergeContexts(JObject.Parse("""{"a":1}"""), new JValue(5));

        Assert.That(merged?.Value<int>(), Is.EqualTo(5));
    }

    [Test]
    public void The_sample_of_a_schema_declaring_nothing_does_not_wipe_the_defaults_it_is_merged_into()
    {
        // What a start node hands over : the sample of an empty schema is a null token rather than a
        // missing one, which is what the guard above is there for.
        JToken? sample = AutomationControl.StartTask.OutputSchema!.ToSampleJson();
        JObject defaults = JObject.Parse("""{"run":"once"}""");

        Assert.That(sample?.Type, Is.EqualTo(JTokenType.Null));
        Assert.That(JToken.DeepEquals(GraphContextResolution.MergeContexts(defaults, sample), defaults), Is.True);
    }
}
