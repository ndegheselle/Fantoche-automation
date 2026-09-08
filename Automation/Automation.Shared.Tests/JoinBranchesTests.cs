using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;

namespace Automation.Shared.Tests;

/// <summary>
/// How a join lets the branches reaching it through, whichever order they arrive in and whatever
/// the ones before them already produced.
///
/// <code>
/// Start ─┬─> A ─┬─> Join
///        └─> B ─┘
/// </code>
/// </summary>
[TestFixture]
internal sealed class JoinBranchesTests
{
    private static (TestGraph Graph, GraphContextResolution Resolution) Build()
    {
        TestGraph graph = new TestGraph()
            .Start()
            .Task("A", "{}")
            .Task("B", "{}")
            .Join("Join", "{}")
            .Connect("Start", "A")
            .Connect("Start", "B")
            .Connect("A", "Join")
            .Connect("B", "Join");

        return (graph, new GraphContextResolution(Guid.NewGuid()));
    }

    [Test]
    public void Join_IsResumedByTheBranchArrivingLast()
    {
        (TestGraph graph, GraphContextResolution resolution) = Build();
        List<BaseGraphTask> previous = [graph["A"], graph["B"]];

        // Both branches are done before either of them reached the join : each of them finds the
        // other one completed, so the state of the branches alone can't tell which one is last.
        TaskInstance a = resolution.CreateInstance(graph["A"], null, EnumTaskState.Completed);
        TaskInstance b = resolution.CreateInstance(graph["B"], null, EnumTaskState.Completed);

        bool first = resolution.TryJoinBranches(graph["Join"], a, previous, out TaskInstance waiting, out _);
        bool second = resolution.TryJoinBranches(graph["Join"], b, previous, out TaskInstance resumed, out List<TaskInstance> branches);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.False, "the branch arriving first holds on the join");
            Assert.That(second, Is.True);
            Assert.That(resumed, Is.SameAs(waiting), "the join is resumed on the instance it waited with");
            Assert.That(branches, Is.EqualTo(new[] { a, b }));
        });
    }

    [Test]
    public void Join_HoldsTheBranchWhileTheOthersHaveYetToProduceAnything()
    {
        (TestGraph graph, GraphContextResolution resolution) = Build();
        List<BaseGraphTask> previous = [graph["A"], graph["B"]];

        TaskInstance a = resolution.CreateInstance(graph["A"], null, EnumTaskState.Completed);
        resolution.CreateInstance(graph["B"], null, EnumTaskState.Progressing);

        bool first = resolution.TryJoinBranches(graph["Join"], a, previous, out _, out _);
        // The second branch arrives with nothing to hand over : the join is left waiting.
        bool second = resolution.TryJoinBranches(graph["Join"], a, previous, out _, out _);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.False);
            Assert.That(second, Is.False);
        });
    }

    [Test]
    public void Join_WaitsForItsBranchesAgainOnTheNextTurn()
    {
        (TestGraph graph, GraphContextResolution resolution) = Build();
        List<BaseGraphTask> previous = [graph["A"], graph["B"]];

        TaskInstance a = resolution.CreateInstance(graph["A"], null, EnumTaskState.Completed);
        TaskInstance b = resolution.CreateInstance(graph["B"], null, EnumTaskState.Completed);

        resolution.TryJoinBranches(graph["Join"], a, previous, out TaskInstance resumed, out _);
        resolution.TryJoinBranches(graph["Join"], b, previous, out _, out _);

        // A loop leading back to the join : its branches walk through it again.
        bool third = resolution.TryJoinBranches(graph["Join"], a, previous, out TaskInstance next, out _);
        bool fourth = resolution.TryJoinBranches(graph["Join"], b, previous, out _, out _);

        Assert.Multiple(() =>
        {
            Assert.That(third, Is.False);
            Assert.That(fourth, Is.True);
            Assert.That(next, Is.Not.SameAs(resumed), "a turn through the join holds an instance of its own");
        });
    }
}
