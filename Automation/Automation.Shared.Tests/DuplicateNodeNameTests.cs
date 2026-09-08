using Automation.Shared.Data.Execution;

namespace Automation.Shared.Tests;

/// <summary>
/// A node several branches lead into reads them by node name, so two of its branches sharing a name
/// leave it unable to say which one it reads. What the graph does about it is what an editor has to
/// refuse beforehand.
/// </summary>
[TestFixture]
public class DuplicateNodeNameTests
{
    [Test]
    public void Two_branches_of_a_join_cannot_share_a_name()
    {
        TestGraph graph = new TestGraph()
            .Start()
            .Task("A", "{}", TestGraph.ObjectSchema("left"))
            .Task("B", "{}", TestGraph.ObjectSchema("right"))
            .Join("J", "{}")
            .Connect("Start", "A")
            .Connect("Start", "B")
            .Connect("A", "J")
            .Connect("B", "J");

        // The very thing renaming a node can produce.
        graph["B"].Metadata.Name = "A";

        Assert.That(() => graph.Preview(), Throws.Exception);
    }

    [Test]
    public void Branches_telling_themselves_apart_resolve()
    {
        TestGraph graph = new TestGraph()
            .Start()
            .Task("A", "{}", TestGraph.ObjectSchema("left"))
            .Task("B", "{}", TestGraph.ObjectSchema("right"))
            .Join("J", """{"l":"$previous.A.left","r":"$previous.B.right"}""")
            .Connect("Start", "A")
            .Connect("Start", "B")
            .Connect("A", "J")
            .Connect("B", "J");

        PreviewResult result = graph.Preview();

        Assert.That(result.Errors("J"), Is.Empty);
    }
}
