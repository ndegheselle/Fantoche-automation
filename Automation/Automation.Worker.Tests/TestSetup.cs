using Automation.Plugins.Shared;

namespace Automation.Worker.Tests;

/// <summary>
/// What every test of the assembly needs of its environment.
/// </summary>
[SetUpFixture]
internal sealed class TestSetup
{
    [OneTimeSetUp]
    public void CheckPlugins()
    {
        // The tasks are loaded from the assembly built next to the tests rather than from a nuget
        // package, see TestPackageManagement.
        Assert.That(
            File.Exists(Path.Combine(AppContext.BaseDirectory, $"{PluginTasks.Package}.dll")),
            Is.True,
            $"[{PluginTasks.Package}.dll] is expected next to the tests.");

        // A plugin is loaded in a context of its own, handing the shared contract over to the
        // default one : ITask has to be the very same type on both sides, so it is loaded here
        // before anything reaches for a task (see PluginLoader.Load).
        Assert.That(typeof(ITask).Assembly.IsDynamic, Is.False);
    }
}
