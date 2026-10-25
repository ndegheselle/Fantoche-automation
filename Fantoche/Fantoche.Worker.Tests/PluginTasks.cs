using Fantoche.Plugins.Pdf;
using Fantoche.Plugins.Shared;
using Fantoche.Plugins.Tables;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.TestPlugins;
using NJsonSchema;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The task definitions (the reusable "blueprints" the graph nodes point to) the tests build their
/// workflows with : the engine is tested with the tasks of Fantoche.TestPlugins, the tables with the
/// ones of Fantoche.Plugins, the package they target being resolved by
/// <see cref="TestPackageManagement"/> rather than by nuget.
/// </summary>
internal static class PluginTasks
{
    /// <summary>The tasks exercising the engine, shipped with no package.</summary>
    public const string Package = "Fantoche.TestPlugins";

    /// <summary>The tasks of the application.</summary>
    public const string TablesPackage = "Fantoche.Plugins";

    /// <summary>
    /// Adds <c>Add</c> to <c>Value</c> and hands over a <see cref="TestResult"/>.
    /// </summary>
    public static readonly AutomationTask Test = Make(
        "Fantoche.TestPlugins.TestTask", "Test",
        JsonSchema.FromType<TestPlugins.TestParameters>(), JsonSchema.FromType<TestResult>());

    /// <summary>Waits, pass-through : what it is given never reaches the next node.</summary>
    public static readonly AutomationTask Delay = Make(
        "Fantoche.TestPlugins.TestDelay", "Delay",
        JsonSchema.FromType<TestDelayParameters>(), new JsonSchema(), passThrough: true);

    public static readonly AutomationTask PassThrough = Make(
        "Fantoche.TestPlugins.PassThroughTask", "PassThrough",
        JsonSchema.FromType<PassThroughParameters>(), new JsonSchema(), passThrough: true);

    /// <summary>Deactivates its output on demand, which closes the branch it stands on.</summary>
    public static readonly AutomationTask Conditional = Make(
        "Fantoche.TestPlugins.ConditionalTask", "Conditional",
        JsonSchema.FromType<ConditionalParameters>(), new JsonSchema(), passThrough: true);

    /// <summary>Closes its branch depending on a value : the two sides of a loop.</summary>
    public static readonly AutomationTask LoopGate = Make(
        "Fantoche.TestPlugins.LoopGateTask", "LoopGate",
        JsonSchema.FromType<LoopGateParameters>(), new JsonSchema(), passThrough: true);

    /// <summary>The user puts items in order on a page : a user interface task.</summary>
    public static readonly AutomationTask Reorder = Make(
        "Fantoche.TestPlugins.UserInterfaces.ReorderTask", "Reorder",
        JsonSchema.FromType<TestPlugins.UserInterfaces.ReorderParameters>(), JsonSchema.FromType<TestPlugins.UserInterfaces.ReorderResult>());

    /// <summary>A task pointing at a class the assembly doesn't hold.</summary>
    public static readonly AutomationTask UnknownClass = Make(
        "Fantoche.TestPlugins.NotATask", "UnknownClass",
        new JsonSchema(), new JsonSchema());

    /// <summary>A task pointing at an assembly that can't be resolved.</summary>
    public static readonly AutomationTask UnknownPackage = Make(
        "Fantoche.TestPlugins.TestTask", "UnknownPackage",
        new JsonSchema(), new JsonSchema(), package: "Fantoche.NotAPackage");

    #region Tables

    public static readonly AutomationTask ImportCsv = Table<ImportCsvTask, ImportCsvParameters, TableRef>("ImportCsv");
    public static readonly AutomationTask PickColumns = Table<PickColumnsTask, PickColumnsParameters, PickColumnsResult>("PickColumns");
    public static readonly AutomationTask MapColumns = Table<MapColumnsTask, MapColumnsParameters, TableRef>("MapColumns");
    public static readonly AutomationTask Merge = Table<MergeTask, MergeParameters, TableRef>("Merge");
    public static readonly AutomationTask Update = Table<UpdateTask, UpdateParameters, TableRef>("Update");
    public static readonly AutomationTask Split = Table<SplitTask, SplitParameters, SplitResult>("Split");
    public static readonly AutomationTask ComposePdf = Table<ComposePdfTask, ComposePdfParameters, ComposePdfResult>("ComposePdf");
    public static readonly AutomationTask ExportPdf = Table<ExportPdfTask, ExportPdfParameters, ExportPdfResult>("ExportPdf");

    /// <summary>A task of Fantoche.Plugins, with the schemas its types declare as adding it from its package does.</summary>
    private static AutomationTask Table<TTask, TInput, TOutput>(string name)
        => Make(typeof(TTask).FullName!, name, JsonSchema.FromType<TInput>(), JsonSchema.FromType<TOutput>(), package: TablesPackage);

    #endregion

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
