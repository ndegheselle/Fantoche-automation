namespace Fantoche.Plugins.Shared;

/// <summary>
/// A task with a page asking the user for its parameters, before it runs. A workflow asks it only
/// where the task is given an "ask the user" node : without one, the task runs unattended. The page
/// always runs in an application, where the user is : a worker reaching it hands it to one through
/// the supervisor.
/// <para>
/// The page reads the parameters of the task as <c>window.fantoche.input</c> and answers them with
/// <c>await window.fantoche.answer(value)</c>, rejected when the answer doesn't fit the parameters of
/// the task. Meanwhile it can call the methods of the task marked with
/// <see cref="PageMethodAttribute"/> : <c>await window.fantoche.call('Name', args)</c>.
/// <c>window.fantoche.close()</c> closes it unanswered, to be opened again later.
/// </para>
/// <para>
/// It only reaches the files of its own folder, and the <see cref="FileRef"/> and
/// <see cref="TableRef"/> of its input or handed back by a method :
/// <list type="bullet">
/// <item><c>window.fantoche.fileUrl(file)</c> : the url of the file, for an <c>img</c>, an
/// <c>iframe</c> or a <c>fetch</c>.</item>
/// <item><c>await window.fantoche.readTable(table, { offset, limit })</c> : <c>{ Columns, Total, Rows }</c>,
/// [limit] rows from [offset] (100 by default, 1000 at most), each by column name.</item>
/// </list>
/// </para>
/// </summary>
public interface ITaskWithPage : ITask
{
    /// <summary>
    /// The folder of the page, relative to the assembly of the task : its <c>index.html</c> is
    /// opened, the rest of the folder served along. Packed next to the assembly, see the
    /// <c>Fantoche.Plugins.Example</c> project.
    /// </summary>
    string PageFolder { get; }
}

/// <summary>
/// A method of a task its page calls while it asks, on an instance of the task :
/// <c>await window.fantoche.call('Name', args)</c>. It takes at most one value, read from [args],
/// and can take the <see cref="ITaskRuntime"/> and a <see cref="CancellationToken"/> along ; what it
/// returns is handed back to the page, the files and tables it holds being reachable from the page
/// from then on. Usually the rules of the task itself, so the page applies them rather than a copy.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PageMethodAttribute : Attribute
{
}
