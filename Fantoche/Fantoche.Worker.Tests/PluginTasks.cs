using Fantoche.Plugins;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The task definitions (the reusable "blueprints" the graph nodes point to) the tests build their
/// workflows with, every one of them targeting a class of the Fantoche.Plugins assembly.
/// <para>
/// Same definitions as the ones the console scenarios are written with
/// (<c>Fantoche.Worker.Console.Scenarios.ScenarioTasks</c>), the package they target being
/// resolved by <see cref="TestPackageManagement"/> rather than by nuget.
/// </para>
/// </summary>
internal static class PluginTasks
{
    public const string Package = "Fantoche.Plugins";

    /// <summary>
    /// Adds <c>Add</c> to <c>Value</c> and hands over a <see cref="TestResult"/>.
    /// </summary>
    public static readonly AutomationTask Test = Make(
        "Fantoche.Plugins.TestTask", "Test",
        JsonSchema.FromType<Plugins.TestParameters>(), JsonSchema.FromType<TestResult>());

    /// <summary>Waits, pass-through : what it is given never reaches the next node.</summary>
    public static readonly AutomationTask Delay = Make(
        "Fantoche.Plugins.TestDelay", "Delay",
        JsonSchema.FromType<TestDelayParameters>(), new JsonSchema(), passThrough: true);

    public static readonly AutomationTask PassThrough = Make(
        "Fantoche.Plugins.PassThroughTask", "PassThrough",
        JsonSchema.FromType<PassThroughParameters>(), new JsonSchema(), passThrough: true);

    /// <summary>Deactivates its output on demand, which closes the branch it stands on.</summary>
    public static readonly AutomationTask Conditional = Make(
        "Fantoche.Plugins.ConditionalTask", "Conditional",
        JsonSchema.FromType<ConditionalParameters>(), new JsonSchema(), passThrough: true);

    /// <summary>Closes its branch depending on a value : the two sides of a loop.</summary>
    public static readonly AutomationTask LoopGate = Make(
        "Fantoche.Plugins.LoopGateTask", "LoopGate",
        JsonSchema.FromType<LoopGateParameters>(), new JsonSchema(), passThrough: true);

    /// <summary>A task pointing at a class the assembly doesn't hold.</summary>
    public static readonly AutomationTask UnknownClass = Make(
        "Fantoche.Plugins.NotATask", "UnknownClass",
        new JsonSchema(), new JsonSchema());

    /// <summary>A task pointing at an assembly that can't be resolved.</summary>
    public static readonly AutomationTask UnknownPackage = Make(
        "Fantoche.Plugins.TestTask", "UnknownPackage",
        new JsonSchema(), new JsonSchema(), package: "Fantoche.NotAPackage");

    /// <summary>
    /// A task definition of its own for every test needing one, so that a graph pointing at a task
    /// no test shares can't be reached by another one.
    /// </summary>
    public static AutomationTask Make(
        string className,
        string name,
        JsonSchema? input,
        JsonSchema? output,
        bool passThrough = false,
        string package = Package)
        => new()
        {
            Id = Guid.NewGuid(),
            Target = new PackageClassTarget()
            {
                Dll = package,
                ClassFullName = className,
                Package = new PackageIdentifier()
                {
                    Id = package,
                    Version = new Version(1, 0, 0)
                }
            },
            Metadata = new ScopedMetadata() { Name = name },
            InputSchema = input,
            OutputSchema = output,
            Settings = new TaskSettings() { IsPassingThrough = passThrough }
        };
}
