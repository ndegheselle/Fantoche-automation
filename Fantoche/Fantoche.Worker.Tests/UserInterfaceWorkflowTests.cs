using Fantoche.Shared.Data.Execution;
using Fantoche.Worker.UserInterfaces;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// A task asking the user while it runs : its page answered by the user (a <see cref="ScriptedUser"/>
/// here), the task waiting meanwhile, then carrying on with the answer.
/// <code>
/// Start ─> Ask (the user reorders) ─> End
/// </code>
/// </summary>
[TestFixture]
internal sealed class UserInterfaceWorkflowTests
{
    private static TestWorkflow BuildAsk(object askUser) => new TestWorkflow("Ask", requireOutput: false)
        .Start()
        .Task("Ask", PluginTasks.Reorder, new { Title = "Sort them", Items = "$previous.Items", AskUser = askUser })
        .End(new { Items = "$previous.Items" })
        .Chain("Start", "Ask", "End");

    private static readonly object Items = new { Items = new[] { "b", "a" }, AskUser = true };

    private static string[]? ItemsOf(WorkflowRun run) => run.Instance.Output?["Items"]?.ToObject<string[]>();

    [Test]
    public async Task TaskNotAskingTheUser_RunsUnattended()
    {
        ScriptedUser user = ScriptedUser.Answering(new { Items = new[] { "x" } });

        WorkflowRun run = await BuildAsk(askUser: false).RunAsync(Items, user: user);

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(user.Asked, Is.Empty);
        });
    }

    [Test]
    public async Task AskingTask_CarriesOnWithTheAnswerOfItsPage()
    {
        WorkflowRun run = await BuildAsk(askUser: true).RunAsync(Items, user: ScriptedUser.Answering(new { Items = new[] { "c" } }));

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "c" }));
            Assert.That(run.States("Ask"), Is.EqualTo(new[] { EnumTaskState.Progressing, EnumTaskState.AwaitingUser, EnumTaskState.Progressing, EnumTaskState.Completed }));
        });
    }

    [Test]
    public async Task Page_IsAskedWithWhatTheTaskGivesIt()
    {
        ScriptedUser user = ScriptedUser.Answering(new { Items = new[] { "c" } });

        await BuildAsk(askUser: true).RunAsync(Items, user: user);

        PageRequest asked = user.Asked.Single();
        Assert.Multiple(() =>
        {
            Assert.That(asked.Input?["Items"]?.ToObject<string[]>(), Is.EqualTo(new[] { "b", "a" }));
            Assert.That(asked.Folder, Does.EndWith(Path.Combine("UserInterfaces", "Reorder")));
        });
    }

    [Test]
    public async Task Page_CallsTheMethodsOfTheTask()
    {
        // The page answers with what the task suggests.
        ScriptedUser user = new(async (request, cancellation) =>
        {
            JToken? sorted = await request.CallAsync("Sorted", request.Input, cancellation);
            return new JObject { ["Items"] = sorted };
        });

        WorkflowRun run = await BuildAsk(askUser: true).RunAsync(Items, user: user);

        Assert.That(ItemsOf(run), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public async Task UnknownPageMethod_IsRefused()
    {
        string? refusal = null;
        ScriptedUser user = new(async (request, cancellation) =>
        {
            try
            {
                await request.CallAsync("Nothing", null, cancellation);
            }
            catch (ArgumentException exception)
            {
                refusal = exception.Message;
            }
            return JToken.FromObject(new { Items = new[] { "c" } });
        });

        await BuildAsk(askUser: true).RunAsync(Items, user: user);

        Assert.That(refusal, Is.EqualTo("[Nothing] isn't a page method of the task."));
    }

    [Test]
    public async Task AnswerNotFittingTheTask_IsRefusedAndThePageAsksOn()
    {
        int answers = 0;
        ScriptedUser user = new((_, _) => Task.FromResult<JToken?>(Interlocked.Increment(ref answers) == 1
            ? JToken.FromObject(new { Items = 3 })
            : null));
        using CancellationTokenSource stop = new();
        Task<WorkflowRun> running = BuildAsk(askUser: true).RunAsync(Items, stop.Token, user);
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
        Task<WorkflowRun> running = BuildAsk(askUser: true).RunAsync(Items, stop.Token, user);
        await user.FirstAskedAsync();

        stop.Cancel();
        WorkflowRun run = await running;

        Assert.Multiple(() =>
        {
            Assert.That(run.States("Ask").Last(), Is.EqualTo(EnumTaskState.Canceled));
            Assert.That(user.Open, Is.Zero, "the page is let go");
        });
    }

    [Test]
    public async Task AskingTheUserWhereNoPageCanBeShown_FailsTheTask()
    {
        WorkflowRun run = await BuildAsk(askUser: true).RunAsync(Items);

        NodeReport ask = run.Of("Ask").Last();
        Assert.Multiple(() =>
        {
            Assert.That(ask.State, Is.EqualTo(EnumTaskState.Failed));
            Assert.That(ask.Output?.ToString(), Does.Contain("'Ask' asks the user, and nothing here shows its page."));
        });
    }

    [Test]
    public async Task WhetherToAsk_IsReadFromTheContext()
    {
        ScriptedUser user = ScriptedUser.Answering(new { Items = new[] { "c" } });

        WorkflowRun run = await BuildAsk(askUser: "$previous.AskUser").RunAsync(Items, user: user);

        Assert.That(user.Asked, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task NestedTaskAskingTheUser_IsAskedWithTheRunAtTheTop()
    {
        TestWorkflow inner = new TestWorkflow("Inner", requireOutput: false)
            .Start()
            .Task("Ask", PluginTasks.Reorder, new { Title = "Sort them", Items = "$previous.Items", AskUser = true })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Ask", "End");
        inner.Workflow.OutputSchema = NJsonSchema.JsonSchema.FromType<Plugins.UserInterfaces.ReorderResult>();
        TestWorkflow outer = new TestWorkflow("Outer", requireOutput: false)
            .Start()
            .Nested("Inner", inner, new { Items = "$previous.Items" })
            .End(new { Items = "$previous.Items" })
            .Chain("Start", "Inner", "End");
        ScriptedUser user = ScriptedUser.Answering(new { Items = new[] { "c" } });

        WorkflowRun run = await outer.RunAsync(Items, user: user);

        Assert.Multiple(() =>
        {
            Assert.That(ItemsOf(run), Is.EqualTo(new[] { "c" }));
            Assert.That(user.Asked.Single().RunId, Is.Not.EqualTo(user.Asked.Single().Instance.ParentInstanceId), "the run at the top, not the nested one");
        });
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }
}
