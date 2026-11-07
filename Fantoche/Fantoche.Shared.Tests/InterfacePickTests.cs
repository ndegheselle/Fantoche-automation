using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Shared.Tests;

/// <summary>
/// Which interface a task or workflow is set up, started and asked with : a wrong pick shows the user
/// a form instead of the interface linked to the task, or the other way around.
/// </summary>
[TestFixture]
internal sealed class InterfacePickTests
{
    private static AutomationInterface Linked(string name) => new(name, Guid.NewGuid())
    {
        Id = Guid.NewGuid(),
        Target = new PackageClassTarget(new PackageIdentifier { Id = "Interfaces" }, $"Interfaces.{name}"),
    };

    [Test]
    public void Default_IsTheForm_WhenNoInterfaceIsLinked()
    {
        AutomationInterface? picked = AutomationInterface.Pick(new TaskSettings(), []);

        Assert.That(picked, Is.Null);
    }

    [Test]
    public void Default_IsTheLinkedInterface()
    {
        AutomationInterface linked = Linked("Composer");

        AutomationInterface? picked = AutomationInterface.Pick(new TaskSettings(), [linked]);

        Assert.That(picked, Is.SameAs(linked));
    }

    [Test]
    public void Default_SkipsAnInterfacePointingAtNoClass()
    {
        AutomationInterface unset = Linked("Unset");
        unset.Target = null;
        AutomationInterface linked = Linked("Composer");

        AutomationInterface? picked = AutomationInterface.Pick(new TaskSettings(), [unset, linked]);

        Assert.That(picked, Is.SameAs(linked));
    }

    [Test]
    public void PickingTheForm_OverridesTheLinkedInterface()
    {
        TaskSettings settings = new() { InterfaceId = AutomationInterface.Form.Id };

        AutomationInterface? picked = AutomationInterface.Pick(settings, [Linked("Composer")]);

        Assert.That(picked, Is.Null);
    }

    [Test]
    public void PickingAnInterface_OverridesTheDefaultOne()
    {
        AutomationInterface first = Linked("First");
        AutomationInterface second = Linked("Second");
        TaskSettings settings = new() { InterfaceId = second.Id };

        AutomationInterface? picked = AutomationInterface.Pick(settings, [first, second]);

        Assert.That(picked, Is.SameAs(second));
    }

    [Test]
    public void PickingAnInterfaceNoLongerLinked_FallsBackOnTheDefault()
    {
        AutomationInterface linked = Linked("Composer");
        TaskSettings settings = new() { InterfaceId = Guid.NewGuid() };

        AutomationInterface? picked = AutomationInterface.Pick(settings, [linked]);

        Assert.That(picked, Is.SameAs(linked));
    }
}
