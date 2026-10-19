using System.Text.Json;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Tests;

/// <summary>
/// An instance crossing the supervisor comes back as it left.
/// </summary>
[TestFixture]
internal sealed class FantocheJsonTests
{
    private static TaskInstance RoundTrip(TaskInstance instance)
    {
        string json = JsonSerializer.Serialize(instance, FantocheJson.Options);
        return JsonSerializer.Deserialize<TaskInstance>(json, FantocheJson.Options)!;
    }

    [Test]
    public void DeactivatedOutput_StaysNull()
    {
        // A null output closes the branch of a workflow, an empty one doesn't.
        TaskInstance instance = new() { Output = null };

        Assert.That(RoundTrip(instance).Output, Is.Null);
    }

    [Test]
    public void TextLookingLikeADate_StaysText()
    {
        TaskInstance instance = new() { Output = new JObject { ["When"] = "2026-10-19T08:00:00Z" } };

        JToken? when = RoundTrip(instance).Output?["When"];

        Assert.Multiple(() =>
        {
            Assert.That(when?.Type, Is.EqualTo(JTokenType.String));
            Assert.That(when?.Value<string>(), Is.EqualTo("2026-10-19T08:00:00Z"));
        });
    }
}
