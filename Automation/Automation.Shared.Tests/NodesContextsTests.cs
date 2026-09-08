using Automation.Shared.Data.Execution;

namespace Automation.Shared.Tests;

/// <summary>
/// What <see cref="GraphExecutionPreview.NodesContexts"/> hands an editor : the ways a node can be
/// reached, which is what the settings overlay shows and resolves a mapping being written against.
/// </summary>
[TestFixture]
public class NodesContextsTests
{
    /// <summary>
    /// <code>
    ///         +--> A --+
    /// Start --+        +--> M (map)
    ///         +--> B --+
    /// </code>
    /// </summary>
    private static TestGraph Diamond(string mapMapping = "{}") => new TestGraph()
        .Start("""{"run":"once"}""")
        .Task("A", "{}", TestGraph.ObjectSchema("left"))
        .Task("B", "{}", TestGraph.ObjectSchema("right"))
        .Map("M", mapMapping)
        .Connect("Start", "A")
        .Connect("Start", "B")
        .Connect("A", "M")
        .Connect("B", "M");

    [Test]
    public void A_node_holds_one_context_per_way_it_can_be_reached()
    {
        PreviewResult result = Diamond().Preview();

        IReadOnlyList<NodePreviewContext> contexts = result.Contexts("M");

        Assert.Multiple(() =>
        {
            Assert.That(contexts, Has.Count.EqualTo(2));
            // Each of them is named by the branch that fed it, which is how the overlay tells the
            // two ways in apart.
            Assert.That(contexts.SelectMany(x => x.Branches), Is.EquivalentTo(new[] { "A", "B" }));
            // And each reads what that branch handed over.
            Assert.That(contexts.Select(x => x.Context["previous"]?["left"] != null), Is.EquivalentTo(new[] { true, false }));
        });
    }

    [Test]
    public void A_context_holds_the_three_roots_a_mapping_can_reference()
    {
        PreviewResult result = Diamond().Preview();

        Assert.That(
            result.Contexts("A").Single().Context.Properties().Select(x => x.Name),
            Is.EquivalentTo(new[] { "previous", "shared", "global" }));
    }

    [Test]
    public void A_join_names_every_branch_it_merges()
    {
        PreviewResult result = new TestGraph()
            .Start()
            .Task("A", "{}", TestGraph.ObjectSchema("left"))
            .Task("B", "{}", TestGraph.ObjectSchema("right"))
            .Join("J", "{}")
            .Connect("Start", "A")
            .Connect("Start", "B")
            .Connect("A", "J")
            .Connect("B", "J")
            .Preview();

        NodePreviewContext context = result.Contexts("J").Single();

        Assert.Multiple(() =>
        {
            // A join reads its branches by node name, so the overlay shows them under one context.
            Assert.That(context.Branches, Is.EquivalentTo(new[] { "A", "B" }));
            Assert.That(context.Context["previous"]?["A"], Is.Not.Null);
            Assert.That(context.Context["previous"]?["B"], Is.Not.Null);
        });
    }

    [Test]
    public void The_start_holds_a_context_of_its_own()
    {
        PreviewResult result = Diamond().Preview();

        NodePreviewContext context = result.Contexts("Start").Single();

        Assert.Multiple(() =>
        {
            // Nothing feeds the start, but its mapping is the defaults of the workflow and those can
            // read the context of the scopes holding it.
            Assert.That(context.Branches, Is.Empty);
            Assert.That(context.Context.Properties().Select(x => x.Name), Does.Contain("global"));
        });
    }

    [Test]
    public void A_context_is_kept_even_when_the_mapping_fails_on_it()
    {
        // The map reads something only one of its two branches hands over.
        PreviewResult result = Diamond("""{"v":"$previous.left"}""").Preview();

        Assert.Multiple(() =>
        {
            // Both ways in are shown : a broken mapping is exactly when the reader needs to see what
            // the node actually reads.
            Assert.That(result.Contexts("M"), Has.Count.EqualTo(2));
            Assert.That(result.Errors("M"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void An_error_names_the_node_it_is_held_against()
    {
        PreviewResult result = Diamond("""{"v":"$previous.left"}""").Preview();
        GraphPreviewError error = result.Errors("M").Single();

        // An edge holds what the node it leads to cannot handle, so the error has to name that node
        // for the editor to say so on the edge.
        Assert.That(error.NodeId, Is.EqualTo(result.Source["M"].Id));
        Assert.That(result.EdgeErrors("B", "M").Single().NodeId, Is.EqualTo(result.Source["M"].Id));
    }
}
