using Automation.Shared.Data.Scoped;

namespace Automation.Shared.Tests;

/// <summary>
/// The schemas of a workflow are declared by the start and the end of its graph. The element carries
/// a copy for whoever reads it without loading the graph, written as the workflow is stored — so what
/// matters is that the copy says exactly what the two nodes do.
/// </summary>
[TestFixture]
public class DeriveSchemasTests
{
    [Test]
    public void The_schemas_of_a_workflow_come_from_its_start_and_its_end()
    {
        TestGraph graph = new TestGraph().Start().End();
        // The start declares what it hands over, the end what reaches it.
        graph["Start"].OutputSchemaJson = TestGraph.ObjectSchema("started");
        graph["End"].InputSchemaJson = TestGraph.ObjectSchema("handedBack");

        AutomationWorkflow workflow = new() { Graph = graph.Graph };
        workflow.DeriveSchemas();

        Assert.Multiple(() =>
        {
            Assert.That(workflow.InputSchema!.Properties.Keys, Is.EquivalentTo(new[] { "started" }));
            Assert.That(workflow.OutputSchema!.Properties.Keys, Is.EquivalentTo(new[] { "handedBack" }));
        });
    }

    [Test]
    public void Deriving_again_follows_what_the_nodes_now_declare()
    {
        TestGraph graph = new TestGraph().Start().End();
        graph["Start"].OutputSchemaJson = TestGraph.ObjectSchema("before");

        AutomationWorkflow workflow = new() { Graph = graph.Graph };
        workflow.DeriveSchemas();

        graph["Start"].OutputSchemaJson = TestGraph.ObjectSchema("after");
        workflow.DeriveSchemas();

        Assert.That(workflow.InputSchema!.Properties.Keys, Is.EquivalentTo(new[] { "after" }));
    }

    [Test]
    public void A_graph_holding_no_boundary_yet_declares_nothing()
    {
        // A workflow being drawn has no start and no end, and the element says so rather than
        // keeping whatever it was last written with.
        AutomationWorkflow workflow = new()
        {
            InputSchemaJson = TestGraph.ObjectSchema("stale"),
            OutputSchemaJson = TestGraph.ObjectSchema("stale"),
        };

        workflow.DeriveSchemas();

        Assert.Multiple(() =>
        {
            Assert.That(workflow.InputSchemaJson, Is.Null);
            Assert.That(workflow.OutputSchemaJson, Is.Null);
        });
    }
}
