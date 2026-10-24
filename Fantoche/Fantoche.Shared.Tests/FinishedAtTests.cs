using Fantoche.Shared.Data.Execution;

namespace Fantoche.Shared.Tests;

/// <summary>
/// When an instance is told finished : once its state ends it, and only then. What the history
/// shows as the end of a run, and how long it took.
/// </summary>
[TestFixture]
internal sealed class FinishedAtTests
{
    [Test]
    public void RunningInstance_IsNotFinished()
    {
        TaskInstance instance = new() { State = EnumTaskState.Pending };

        instance.State = EnumTaskState.Progressing;

        Assert.That(instance.FinishedAt, Is.Null);
    }

    [Test]
    public void EndedInstance_IsFinished()
    {
        TaskInstance instance = new() { State = EnumTaskState.Progressing };

        instance.State = EnumTaskState.Completed;

        Assert.That(instance.FinishedAt, Is.Not.Null);
    }

    [Test]
    public void ResumedInstance_IsNotFinishedAnymore()
    {
        TaskInstance instance = new() { State = EnumTaskState.Progressing };
        instance.State = EnumTaskState.Suspended;

        instance.State = EnumTaskState.Progressing;

        Assert.That(instance.FinishedAt, Is.Null);
    }

    [Test]
    public void InstanceSavedAgain_KeepsWhenItFinished()
    {
        TaskInstance instance = new() { State = EnumTaskState.Progressing };
        instance.State = EnumTaskState.Completed;
        DateTime? finished = instance.FinishedAt;

        instance.State = EnumTaskState.Completed;

        Assert.That(instance.FinishedAt, Is.EqualTo(finished));
    }
}
