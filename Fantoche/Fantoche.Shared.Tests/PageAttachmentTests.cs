using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Shared.Tests;

/// <summary>
/// Which page can be attached under which task : the page edits the parameters of the task, so a
/// wrong attachment would hand the task parameters of another shape, or ask the user twice.
/// </summary>
[TestFixture]
internal sealed class PageAttachmentTests
{
    private const string Parameters = """{"type":"object","properties":{"Items":{"type":"array"}}}""";
    private const string OtherParameters = """{"type":"object","properties":{"Path":{"type":"string"}}}""";

    private readonly TasksGraph _graph = new();

    private GraphTask Task(string inputSchema = Parameters)
    {
        GraphTask node = new(new AutomationTask("Task", Guid.Empty) { Id = Guid.NewGuid(), InputSchemaJson = inputSchema });
        _graph.Nodes.Add(node);
        return node;
    }

    private GraphUserInterface Page(string inputSchema = Parameters)
    {
        AutomationTask page = new("Page", Guid.Empty)
        {
            Id = Guid.NewGuid(),
            InputSchemaJson = inputSchema,
            Target = new PackageClassTarget { IsUserInterface = true },
        };
        GraphUserInterface node = (GraphUserInterface)BaseGraphTask.For(page);
        _graph.Nodes.Add(node);
        return node;
    }

    [Test]
    public void AttachedPage_IsThePageOfItsTask()
    {
        GraphTask task = Task();
        GraphUserInterface page = Page();

        _graph.Attach(task, page);

        Assert.Multiple(() =>
        {
            Assert.That(_graph.GetUserInterfaceOf(task), Is.SameAs(page));
            Assert.That(_graph.GetTaskAskedBy(page), Is.SameAs(task));
            Assert.That(_graph.GetNext(task), Is.Empty, "a page is no branch of its task");
        });
    }

    [Test]
    public void PageEditingOtherParameters_CantBeAttached()
    {
        GraphTask task = Task();
        GraphUserInterface page = Page(OtherParameters);

        Assert.That(_graph.CanConnect(task.UserInterface!, page.UserInterface!), Is.False);
    }

    [Test]
    public void SecondPage_CantBeAttachedToTheSameTask()
    {
        GraphTask task = Task();
        _graph.Attach(task, Page());

        Assert.That(_graph.CanConnect(task.UserInterface!, Page().UserInterface!), Is.False);
    }

    [Test]
    public void Page_CantBeAttachedToTwoTasks()
    {
        GraphUserInterface page = Page();
        _graph.Attach(Task(), page);

        Assert.That(_graph.CanConnect(Task().UserInterface!, page.UserInterface!), Is.False);
    }

    [Test]
    public void TaskSlot_CantTakeAnotherTask()
    {
        GraphTask task = Task();

        Assert.That(_graph.CanConnect(task.UserInterface!, Task().UserInterface!), Is.False);
    }

    [Test]
    public void PageNotYetInTheGraph_IsCheckedAgainstItsTask()
    {
        GraphTask task = Task();
        GraphUserInterface page = Page();
        _graph.Nodes.Remove(page);

        Assert.That(_graph.GetAttachError(task, page), Is.Null, "a page dropped on its task is checked before being added");
    }

    [Test]
    public void RefusedAttachment_TellsWhy()
    {
        GraphTask task = Task();
        _graph.Attach(task, Page());

        Assert.Multiple(() =>
        {
            Assert.That(_graph.GetAttachError(Task(), Page(OtherParameters)), Does.Contain("doesn't edit the parameters"));
            Assert.That(_graph.GetAttachError(task, Page()), Does.Contain("already has a page"));
            Assert.That(_graph.GetAttachError(task, Task()), Does.Contain("is not a page"));
        });
    }
}
