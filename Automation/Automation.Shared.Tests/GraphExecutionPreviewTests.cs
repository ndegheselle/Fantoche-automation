using Automation.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Tests;

[TestFixture]
public class GraphExecutionPreviewTests
{
    /// <summary>
    /// The graph the per-branch preview was designed around :
    /// <code>
    ///                        +--> A --+
    /// Start --> S (share) ---+        +--> M (map) --> T --> End
    ///                        +--> B --+
    /// </code>
    /// A hands over <c>{ data: { url } }</c> and B hands over <c>{ data: string }</c>, so M maps them
    /// into two different shapes and T only holds up on the one that came through A. S feeds
    /// <c>$shared.token</c>, which T reads on top of what its branch carries.
    /// </summary>
    private static TestGraph Diamond() => new TestGraph()
        .Start("""{"run":"once"}""")
        .Share("S", """{"token":"abc"}""")
        .Task("A", """{"from":"$previous.run"}""", TestGraph.NestedDataSchema)
        .Task("B", "{}", TestGraph.FlatDataSchema)
        .Map("M", """{"value":"$previous.data"}""")
        .Task("T", """{"target":"$previous.value.url","token":"$shared.token"}""", TestGraph.FlatDataSchema)
        .End()
        .Connect("Start", "S")
        .Connect("S", "A")
        .Connect("S", "B")
        .Connect("A", "M")
        .Connect("B", "M")
        .Connect("M", "T")
        .Connect("T", "End");

    [Test]
    public void A_control_reached_by_two_branches_is_previewed_once_per_shape_they_carry()
    {
        PreviewResult result = Diamond().Preview();

        // What M hands over is its mapping resolved against one branch, so it has as many shapes as
        // the branches reaching it carry — an object through A, a string through B.
        IEnumerable<JTokenType?> shapes = result.Resolved("M").Select(x => x.Output?["value"]?.Type);

        Assert.That(result.Resolved("M"), Has.Count.EqualTo(2));
        Assert.That(shapes, Is.EquivalentTo(new[] { JTokenType.Object, JTokenType.String }));
    }

    [Test]
    public void A_node_a_single_edge_leads_to_is_previewed_once_per_shape_that_edge_carries()
    {
        PreviewResult result = Diamond().Preview();

        // T is reached by one edge only : what tells its two contexts apart is what M handed over,
        // which is why counting edges is not enough to find the error below.
        Assert.That(result.Resolved("T"), Has.Count.EqualTo(1));
        Assert.That(result.Errors("T"), Has.Count.EqualTo(1));
    }

    [Test]
    public void A_node_failing_on_one_branch_only_is_blamed_on_that_branch()
    {
        PreviewResult result = Diamond().Preview();
        GraphPreviewError error = result.Errors("T").Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Preview.NodesErrors, Has.Count.EqualTo(1));
            Assert.That(error.Message, Does.Contain("previous.value.url"));
            Assert.That(result.Path(error), Is.EqualTo(new[] { "Start->S", "S->B", "B->M", "M->T" }));
            Assert.That(result.Divergence(error), Is.EqualTo("B->M"));
        });
    }

    [Test]
    public void An_edge_holds_what_the_node_it_leads_to_cannot_handle()
    {
        PreviewResult result = Diamond().Preview();

        Assert.Multiple(() =>
        {
            Assert.That(result.Preview.EdgesErrors, Has.Count.EqualTo(1));
            Assert.That(result.EdgeErrors("B", "M"), Has.Count.EqualTo(1));
            // The branch that holds up is left alone.
            Assert.That(result.EdgeErrors("A", "M"), Is.Empty);
        });
    }

    [Test]
    public void A_node_failing_whichever_branch_reaches_it_is_not_blamed_on_an_edge()
    {
        PreviewResult result = new TestGraph()
            .Start()
            .Task("A", """{"x":"$previous.missing"}""")
            .End()
            .Connect("Start", "A")
            .Connect("A", "End")
            .Preview();

        Assert.Multiple(() =>
        {
            Assert.That(result.Errors("A").Single().DivergenceEdge, Is.Null);
            // Nothing on the path branches, so there is nothing to draw in red but the node.
            Assert.That(result.Preview.EdgesErrors, Is.Empty);
        });
    }

    [Test]
    public void A_node_broken_on_every_branch_reaching_it_holds_one_error_per_branch()
    {
        PreviewResult result = new TestGraph()
            .Start()
            .Task("A", "{}", TestGraph.NestedDataSchema)
            .Task("B", "{}", TestGraph.FlatDataSchema)
            .Map("M", """{"v":"$previous.missing"}""")
            .Connect("Start", "A")
            .Connect("Start", "B")
            .Connect("A", "M")
            .Connect("B", "M")
            .Preview();

        Assert.Multiple(() =>
        {
            Assert.That(result.Errors("M"), Has.Count.EqualTo(2));
            Assert.That(result.EdgeErrors("A", "M"), Has.Count.EqualTo(1));
            Assert.That(result.EdgeErrors("B", "M"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void The_shared_context_walks_down_the_branches_a_share_feeds()
    {
        PreviewResult result = Diamond().Preview();

        Assert.Multiple(() =>
        {
            // T reads $shared.token four nodes after the share that feeds it.
            Assert.That(result.AllMessages.Where(x => x.Contains("shared")), Is.Empty);
            Assert.That(result.Resolved("T"), Has.Count.EqualTo(1));
            Assert.That(result.Resolved("End"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void A_control_hands_over_its_resolved_mapping()
    {
        PreviewResult result = Diamond().Preview();
        TaskInstance map = result.Resolved("M").First();

        Assert.Multiple(() =>
        {
            Assert.That(JToken.DeepEquals(map.Output, map.Parameters), Is.True);
            Assert.That(map.Output?["value"], Is.Not.Null);
        });
    }

    [Test]
    public void The_mapping_of_the_end_control_is_previewed()
    {
        PreviewResult result = new TestGraph()
            .Start()
            .Task("A", "{}", TestGraph.FlatDataSchema)
            .End("""{"out":"$previous.missing"}""")
            .Connect("Start", "A")
            .Connect("A", "End")
            .Preview();

        Assert.That(result.Errors("End").Single().Message, Does.Contain("previous.missing"));
    }

    [Test]
    public void A_join_is_previewed_once_every_branch_before_it_reached_it()
    {
        PreviewResult result = new TestGraph()
            .Start()
            .Task("A", "{}", TestGraph.ObjectSchema("left"))
            .Task("B", "{}", TestGraph.ObjectSchema("right"))
            .Join("J", """{"l":"$previous.A.left","r":"$previous.B.right"}""")
            .End()
            .Connect("Start", "A")
            .Connect("Start", "B")
            .Connect("A", "J")
            .Connect("B", "J")
            .Connect("J", "End")
            .Preview();

        Assert.Multiple(() =>
        {
            // A join reads its branches by node name, and resolves once they are all in.
            Assert.That(result.Errors("J"), Is.Empty);
            Assert.That(result.Resolved("J"), Has.Count.EqualTo(1));
            Assert.That(result.Resolved("End"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void The_walk_terminates_on_a_graph_looping_back_on_itself()
    {
        TestGraph graph = Diamond();
        graph.Connect("T", "A");

        PreviewResult result = graph.Preview();

        Assert.Multiple(() =>
        {
            Assert.That(result.Preview.IsTruncated, Is.False);
            // A holds up when the share feeds it and breaks when T loops back with something else,
            // which is the loop being walked exactly as far as it carries something new.
            Assert.That(result.Divergence(result.Errors("A").Single()), Is.EqualTo("T->A"));
        });
    }

    [Test]
    public void A_node_reached_more_ways_than_the_cap_allows_truncates_the_preview()
    {
        TestGraph graph = new TestGraph()
            .Start()
            .Map("M", "{}");

        // One branch more than the cap, each handing over something of its own.
        for (int i = 0; i <= GraphExecutionPreview.MaxContextsPerNode; i++)
        {
            string name = $"T{i}";
            graph.Task(name, "{}", TestGraph.ObjectSchema($"field{i}"))
                .Connect("Start", name)
                .Connect(name, "M");
        }

        PreviewResult result = graph.Preview();

        Assert.Multiple(() =>
        {
            Assert.That(result.Preview.IsTruncated, Is.True);
            Assert.That(result.Resolved("M"), Has.Count.EqualTo(GraphExecutionPreview.MaxContextsPerNode));
        });
    }

    [Test]
    public void A_graph_that_was_not_refreshed_is_refused()
    {
        Data.Graph.TasksGraph graph = new();

        Assert.That(
            () => new GraphExecutionPreview().BuildSamples(graph, new GraphContextResolution()),
            Throws.Exception);
    }
}
