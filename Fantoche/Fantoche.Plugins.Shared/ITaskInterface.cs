using Newtonsoft.Json.Linq;

namespace Fantoche.Plugins.Shared;

/// <summary>Where an interface is shown.</summary>
public enum EnumInterfaceMode
{
    /// <summary>
    /// Outside a run, nothing having run yet : <see cref="TaskInterfaceContext.Runtime"/> is null.
    /// <list type="bullet">
    /// <item>In the editor, setting the node of a task up : it edits the mapping the node runs with,
    /// a field pointing at what the node reads holding a reference ("$previous.Table") rather than a
    /// value (see <see cref="TaskInterfaceContext.References"/>).</item>
    /// <item>Starting or scheduling a workflow or a task on its own : it edits the parameters it
    /// starts with, nothing to point at.</item>
    /// </list>
    /// </summary>
    Setup,

    /// <summary>In a run, asking the user for the parameters of the task before it runs.</summary>
    Execution,
}

/// <summary>
/// The interface of a task or a workflow : what the user edits its parameters with, outside a run
/// (<see cref="EnumInterfaceMode.Setup"/>) and in a run (<see cref="EnumInterfaceMode.Execution"/>).
/// An element of the workflows points at one (<c>BaseAutomationTask.Interface</c>), the form
/// generated from the schema of its parameters being shown otherwise, and in the modes the
/// interface doesn't handle. Mark it with <see cref="TaskInterfaceAttribute"/> to tell which task
/// it is the interface of.
/// <para>
/// It lives in a project of its own, for Windows (<c>net10.0-windows</c>, WPF), packed apart from
/// the tasks : the workers load the tasks on any system and never the interfaces. It can reference
/// the project of its tasks to read their parameters and call their rules typed : its package
/// carries the assembly of the tasks it was built against, both loaded together per version of
/// the interface. See <c>Fantoche.Plugins.Example.Ui</c>.
/// </para>
/// <para>
/// The application shows the view between the name of the node and its own buttons, the error
/// refusing an answer above them : the view only holds what the user edits. A new instance is
/// created each time it is shown.
/// </para>
/// </summary>
public interface ITaskInterface
{
    /// <summary>Whether it is shown in [mode], the generated form being shown otherwise.</summary>
    bool Handles(EnumInterfaceMode mode);

    /// <summary>
    /// The view, built once, on the thread of the interface : a WPF <c>FrameworkElement</c>. Typed
    /// as an object so this assembly stays free of WPF, the workers loading it on any system.
    /// </summary>
    object Show(TaskInterfaceContext context);

    /// <summary>
    /// What the user confirmed : the parameters, or the mapping in the editor. The application
    /// checks it against the schema of the parameters before taking it.
    /// </summary>
    /// <exception cref="TaskInterfaceException">The user has something left to fix, the message telling what.</exception>
    Task<JToken?> ReadAsync();
}

/// <summary>
/// The task an <see cref="ITaskInterface"/> edits the parameters of : the application offers the
/// interface to the elements running that task, as the child of the task in the tree.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TaskInterfaceAttribute(Type task) : Attribute
{
    public Type Task { get; } = task;
}

/// <summary>What an interface is shown with.</summary>
public sealed class TaskInterfaceContext
{
    public required EnumInterfaceMode Mode { get; init; }

    /// <summary>
    /// The parameters in a run ; in the editor, the mapping of the node ; as it is started or
    /// scheduled, the parameters given so far. Null when there are none yet.
    /// </summary>
    public required JToken? Parameters { get; init; }

    /// <summary>What a field can point at instead of holding a value, in the editor only.</summary>
    public IReadOnlyList<TaskReference> References { get; init; } = [];

    /// <summary>
    /// What the rules of the task are handed in a run, its storage first : the tables and files the
    /// parameters point at. Null in <see cref="EnumInterfaceMode.Setup"/>, where nothing ran.
    /// </summary>
    public ITaskRuntime? Runtime { get; init; }

    /// <summary>The parameters read as [T], the class of the parameters of the task.</summary>
    public T? ParametersAs<T>() => Parameters == null ? default : Parameters.ToObject<T>();
}

/// <summary>
/// A value the node reads, which a field of its mapping can point at : "$previous.Table". [Type] is
/// the JSON type it holds as the editor previews it ("object", "string"...), null when unknown (the
/// columns of a row).
/// </summary>
public sealed record TaskReference(string Reference, string? Type);

/// <summary>Refuses what the user confirmed, the message telling what is left to fix.</summary>
public sealed class TaskInterfaceException(string message) : Exception(message);
