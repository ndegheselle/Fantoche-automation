using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Shared.Tests;

/// <summary>
/// Which "ask the user" node can be attached under which task : it shows the page of a task, so a
/// wrong attachment would hand the task the answer to another task's page, or ask the user twice.
/// </summary>
[TestFixture]
internal sealed class PageAttachmentTests
{
    private readonly TasksGraph _graph = new();

    private static AutomationTask TaskWithPage() => new("Task", Guid.Empty)
    {
        Id = Guid.NewGuid(),
        InputSchemaJson = """{"type":"object","properties":{"Items":{"type":"array"}}}""",
        Target = new PackageClassTarget { HasPage = true },
    };

    private GraphTask Node(AutomationTask task)
    {
        GraphTask node = new(task);
        _graph.Nodes.Add(node);
        return node;
    }

    private GraphUserInterface Asking(AutomationTask task)
    {
        GraphUserInterface node = new(task);
        _graph.Nodes.Add(node);
        return node;
    }

    [Test]
    public void AttachedNode_AsksForItsTask()
    {
        AutomationTask reorder = TaskWithPage();
        GraphTask task = Node(reorder);
        GraphUserInterface page = Asking(reorder);

        _graph.Attach(task, page);

        Assert.Multiple(() =>
        {
            Assert.That(_graph.GetUserInterfaceOf(task), Is.SameAs(page));
            Assert.That(_graph.GetTaskAskedBy(page), Is.SameAs(task));
            Assert.That(_graph.GetNext(task), Is.Empty, "asking the user is no branch of its task");
        });
    }

    [Test]
    public void TaskWithoutAPage_TakesNoAskingNode()
    {
        GraphTask task = Node(new AutomationTask("Task", Guid.Empty) { Id = Guid.NewGuid(), InputSchemaJson = "{}" });

        Assert.That(task.UserInterface, Is.Null);
    }

    [Test]
    public void NodeShowingThePageOfAnotherTask_CantBeAttached()
    {
        GraphTask task = Node(TaskWithPage());
        GraphUserInterface page = Asking(TaskWithPage());

        Assert.That(_graph.CanConnect(task.UserInterface!, page.UserInterface!), Is.False);
    }

    [Test]
    public void SecondAskingNode_CantBeAttachedToTheSameTask()
    {
        AutomationTask reorder = TaskWithPage();
        GraphTask task = Node(reorder);
        _graph.Attach(task, Asking(reorder));

        Assert.That(_graph.CanConnect(task.UserInterface!, Asking(reorder).UserInterface!), Is.False);
    }

    [Test]
    public void AskingNode_CantBeAttachedToTwoTasks()
    {
        AutomationTask reorder = TaskWithPage();
        GraphUserInterface page = Asking(reorder);
        _graph.Attach(Node(reorder), page);

        Assert.That(_graph.CanConnect(Node(reorder).UserInterface!, page.UserInterface!), Is.False);
    }

    [Test]
    public void TaskSlot_CantTakeAnotherTask()
    {
        AutomationTask reorder = TaskWithPage();
        GraphTask task = Node(reorder);

        Assert.That(_graph.CanConnect(task.UserInterface!, Node(reorder).UserInterface!), Is.False);
    }

    [Test]
    public void AskingNodeNotYetInTheGraph_IsCheckedAgainstItsTask()
    {
        AutomationTask reorder = TaskWithPage();
        GraphTask task = Node(reorder);
        GraphUserInterface page = new(reorder);

        Assert.That(_graph.GetAttachError(task, page), Is.Null, "a node added under its task is checked before being added");
    }

    [Test]
    public void RefusedAttachment_TellsWhy()
    {
        AutomationTask reorder = TaskWithPage();
        GraphTask task = Node(reorder);
        _graph.Attach(task, Asking(reorder));

        Assert.Multiple(() =>
        {
            Assert.That(_graph.GetAttachError(Node(reorder), Asking(TaskWithPage())), Does.Contain("page of another task"));
            Assert.That(_graph.GetAttachError(task, Asking(reorder)), Does.Contain("already asks the user"));
            Assert.That(_graph.GetAttachError(task, Node(reorder)), Does.Contain("doesn't ask the user"));
        });
    }

    [Test]
    public void InstanceOfAnAskingNode_AsksTheUser()
    {
        AutomationTask reorder = TaskWithPage();
        GraphContextResolution resolution = new(Guid.NewGuid());

        TaskInstance asking = resolution.CreateInstance(Asking(reorder), null);
        TaskInstance running = resolution.CreateInstance(Node(reorder), null);

        Assert.Multiple(() =>
        {
            Assert.That(asking.AsksUser, Is.True);
            Assert.That(running.AsksUser, Is.False);
            Assert.That(asking.TaskId, Is.EqualTo(reorder.Id), "the page shown is the one of the task");
        });
    }
}
