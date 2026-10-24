using Fantoche.Plugins.Shared;
using Fantoche.Plugins.UserInterfaces;
using NJsonSchema;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The answers the review pages send, what they were given handed back : refused, the page could
/// never be confirmed.
/// </summary>
[TestFixture]
internal sealed class ReviewAnswersTests
{
    [Test]
    public void TableReview_AcceptsTheTableItWasGiven()
    {
        TableRef table = new() { Id = $"t_{Guid.NewGuid():N}", Columns = [new() { Name = "name", Type = "VARCHAR" }] };
        JToken answer = JToken.FromObject(new { Table = table });

        var errors = JsonSchema.FromType<TableReviewResult>().Validate(answer);

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void FilesReview_AcceptsTheFilesItKept()
    {
        FileRef file = new() { Id = $"f_{Guid.NewGuid():N}", Name = "invoice.pdf" };
        JToken answer = JToken.FromObject(new { Files = new[] { file } });

        var errors = JsonSchema.FromType<FilesReviewResult>().Validate(answer);

        Assert.That(errors, Is.Empty);
    }
}
