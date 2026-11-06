using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Fantoche.Worker.UserInterfaces;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// A task with a page attached : the page asks the user for the parameters of the task (a
/// <see cref="ScriptedUser"/> answering here), the task waiting meanwhile, then running with them.
/// <code>
/// Start ─> Ask (the items as given) ─> End
///           │
///          Page (the user reorders them)
/// </code>
/// </summary>
[TestFixture]
internal sealed class UserInterfaceWorkflowTests
{
    private static TestWorkflow BuildAsk(bool withPage = true)
    {
        TestWorkflow workflow = new TestWorkflow("Ask", requireOutput: false)
            .Start()
            .Task("Ask", PluginTasks.Reorder, new { Title = "Sort them", Items = "$previous.Items" })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Ask", "End");
        return withPage ? workflow.Ui("Page", of: "Ask") : workflow;
    }

    private static readonly object Items = new { Items = new[] { "b", "a" } };

    private static object Answer(params string[] items) => new { Title = "Sort them", Items = items };

    private static string[]? ItemsOf(WorkflowRun run) => run.Instance.Output?["Items"]?.ToObject<string[]>();

    [Test]
    public async Task TaskWithoutPage_RunsUnattended()
    {
        ScriptedUser user = ScriptedUser.Answering(Answer("x"));

        WorkflowRun run = await BuildAsk(withPage: false).RunAsync(Items, user: user);

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(user.Asked, Is.Empty);
        });
    }

    [Test]
    public async Task TaskWithPage_RunsWithTheAnswerOfItsPage()
    {
        WorkflowRun run = await BuildAsk().RunAsync(Items, user: ScriptedUser.Answering(Answer("c")));

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "c" }));
            Assert.That(run.States("Page"), Is.EqualTo(new[] { EnumTaskState.Progressing, EnumTaskState.AwaitingUser, EnumTaskState.Progressing, EnumTaskState.Completed }));
            Assert.That(run.States("Ask"), Is.EqualTo(new[] { EnumTaskState.Progressing, EnumTaskState.Completed }));
        });
    }

    [Test]
    public async Task Page_IsAskedWithTheParametersOfItsTask()
    {
        ScriptedUser user = ScriptedUser.Answering(Answer("c"));

        await BuildAsk().RunAsync(Items, user: user);

        PageRequest asked = user.Asked.Single();
        Assert.Multiple(() =>
        {
            Assert.That(asked.Input?["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "b", "a" }));
            // What the application shows it with : the interface of the task, or the form of its parameters.
            Assert.That(asked.Task.Target?.ClassFullName, Is.EqualTo("Fantoche.Plugins.UserInterfaces.ReorderTask"));
        });
    }

    [Test]
    public async Task AnswerNotFittingTheTask_IsRefusedAndThePageAsksOn()
    {
        int answers = 0;
        ScriptedUser user = new((_, _) => Task.FromResult<JToken?>(Interlocked.Increment(ref answers) == 1
            ? JToken.FromObject(new { Items = 3 })
            : null));
        using CancellationTokenSource stop = new();
        Task<WorkflowRun> running = BuildAsk().RunAsync(Items, stop.Token, user);
        await user.FirstAskedAsync();
        await WaitForAsync(() => user.Refusals.Count > 0);

        bool isStillAsking = user.Open == 1;
        stop.Cancel();
        await running;

        Assert.Multiple(() =>
        {
            Assert.That(user.Refusals.Single(), Does.StartWith("The answer doesn't fit what the task expects"));
            Assert.That(isStillAsking, Is.True);
        });
    }

    [Test]
    public async Task CanceledRun_StopsAsking()
    {
        ScriptedUser user = ScriptedUser.Away();
        using CancellationTokenSource stop = new();
        Task<WorkflowRun> running = BuildAsk().RunAsync(Items, stop.Token, user);
        await user.FirstAskedAsync();

        stop.Cancel();
        WorkflowRun run = await running;

        Assert.Multiple(() =>
        {
            Assert.That(run.States("Page").Last(), Is.EqualTo(EnumTaskState.Canceled));
            Assert.That(run.States("Ask"), Is.Empty, "the task never runs");
            Assert.That(user.Open, Is.Zero, "the page is let go");
        });
    }

    [Test]
    public async Task PageWhereNoPageCanBeShown_FailsAndTheTaskNeverRuns()
    {
        WorkflowRun run = await BuildAsk().RunAsync(Items);

        NodeReport page = run.Of("Page").Last();
        Assert.Multiple(() =>
        {
            Assert.That(page.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(page.Output?.ToString(), Does.Contain("'Page' asks the user, and nothing here can show it."));
            Assert.That(run.States("Ask"), Is.Empty);
        });
    }

    [Test]
    public async Task ResumedAfterThePageWasAnswered_RunsTheTaskWithTheAnswerWithoutAskingAgain()
    {
        TestWorkflow workflow = BuildAsk();
        (WorkflowInstance resumed, IReadOnlyList<TaskInstance> stored) = await CrashAsync(workflow, ScriptedUser.Answering(Answer("c")));
        // Cut short right after the answer : the task never started.
        resumed.StoredInstances = [.. stored.Where(x => x.NodeName is "Start" or "Page")];
        ScriptedUser user = ScriptedUser.Answering(Answer("d"));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(resumed, user: user);

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "c" }));
            Assert.That(user.Asked, Is.Empty);
        });
    }

    [Test]
    public async Task ResumedWhileThePageWasAsking_AsksAgainThenRunsTheTask()
    {
        TestWorkflow workflow = BuildAsk();
        (WorkflowInstance resumed, IReadOnlyList<TaskInstance> stored) = await CrashAsync(workflow, ScriptedUser.Answering(Answer("c")));
        // Cut short while the user was asked : the page was left suspended.
        TaskInstance page = stored.Single(x => x.NodeName == "Page");
        page.State = EnumTaskState.Suspended;
        page.Output = null;
        resumed.StoredInstances = [.. stored.Where(x => x.NodeName == "Start"), page];
        ScriptedUser user = ScriptedUser.Answering(Answer("d"));

        WorkflowRun run = await WorkflowRun.ExecuteAsync(resumed, user: user);

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "d" }));
            Assert.That(user.Asked, Has.Count.EqualTo(1));
            Assert.That(run.CompletionsOf("Ask"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task NestedPage_IsAskedWithTheRunAtTheTop()
    {
        TestWorkflow inner = new TestWorkflow("Inner", requireOutput: false)
            .Start()
            .Task("Ask", PluginTasks.Reorder, new { Title = "Sort them", Items = "$previous.Items" })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Ask", "End")
            .Ui("Page", of: "Ask");
        inner.Workflow.OutputSchema = NJsonSchema.JsonSchema.FromType<Plugins.UserInterfaces.ReorderResult>();
        TestWorkflow outer = new TestWorkflow("Outer", requireOutput: false)
            .Start()
            .Nested("Inner", inner, new { Items = "$previous.Items" })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Inner", "End");
        ScriptedUser user = ScriptedUser.Answering(Answer("c"));

        WorkflowRun run = await outer.RunAsync(Items, user: user);

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "c" }));
            Assert.That(user.Asked.Single().RunId, Is.Not.EqualTo(user.Asked.Single().Instance.ParentInstanceId), "the run at the top, not the nested one");
        });
    }

    /// <summary>
    /// Run [workflow] to its end, [user] answering : the run to resume, as it stood before it
    /// started, and what the history held of its nodes once over, for a test to cut short.
    /// </summary>
    private static async Task<(WorkflowInstance Resumed, IReadOnlyList<TaskInstance> Stored)> CrashAsync(TestWorkflow workflow, ScriptedUser user)
    {
        WorkflowInstance instance = workflow.Instance(Items);
        TaskInstance storedRun = FantocheJson.Copy<TaskInstance>(instance);
        WorkflowRun run = await WorkflowRun.ExecuteAsync(instance, user: user);

        storedRun.State = EnumTaskState.Suspended;
        WorkflowInstance resumed = WorkflowInstance.From(workflow.Refreshed(), storedRun);
        return (resumed, run.Stored);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }
}
