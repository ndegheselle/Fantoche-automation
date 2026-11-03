using Fantoche.Shared.Data;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Tests;

/// <summary>
/// References written as the items of a list : each one replaced by what it points to, in place.
/// </summary>
[TestFixture]
public class ListReferencesTests
{
    private static readonly JObject Context = new()
    {
        ["previous"] = new JObject
        {
            ["A"] = new JObject { ["Message"] = "a" },
            ["B"] = new JObject { ["Message"] = "b" },
        }
    };

    [Test]
    public void Every_item_of_a_list_of_references_is_resolved()
    {
        JToken template = JToken.FromObject(new { Items = new[] { "$previous.A.Message", "$previous.B.Message" } });

        ReferenceReplaceResult result = ReferencesHandler.ReplaceReferences(template, Context);

        Assert.That(result.Replaced["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "a", "b" }));
    }
}
