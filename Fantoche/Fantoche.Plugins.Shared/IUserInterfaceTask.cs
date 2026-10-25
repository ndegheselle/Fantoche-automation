namespace Fantoche.Plugins.Shared;

/// <summary>
/// A task that can ask the user through a page of its own while it runs, when its input says so
/// (see <see cref="UserInterface.SchemaKey"/>) : otherwise it runs unattended, as any other task.
/// Such a task always runs in an application, where the user is : a worker reaching it hands it to
/// one through the supervisor.
/// <para>
/// While it runs, <see cref="ITaskRuntime.UserInterface"/> shows the page and waits for its answer
/// (see <see cref="IUserInterface.AskAsync"/>). The page reads what it is asked with as
/// <c>window.fantoche.input</c> and answers with <c>await window.fantoche.answer(value)</c>, rejected
/// when the answer doesn't fit what the task expects. Meanwhile it can call the methods of the task
/// marked with <see cref="PageMethodAttribute"/> : <c>await window.fantoche.call('Name', args)</c>.
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
public interface IUserInterfaceTask : ITask
{
    /// <summary>
    /// The folder of the page, relative to the assembly of the task : its <c>index.html</c> is
    /// opened, the rest of the folder served along. Packed next to the assembly, see the
    /// <c>Fantoche.Plugins.Example</c> project.
    /// </summary>
    string PageFolder { get; }
}

/// <summary>
/// Whether a user interface task asks the user : a boolean property of its input marked with
/// <c>[JsonSchemaExtensionData(UserInterface.SchemaKey, true)]</c>. Resolved as any other value, so
/// a workflow decides it from its context ("$previous.AskUser"), and a schedule runs it unattended.
/// </summary>
public static class UserInterface
{
    public const string SchemaKey = "x-fantoche-ask-user";
}

/// <summary>
/// The page of a task while it runs, see <see cref="IUserInterfaceTask"/>.
/// </summary>
public interface IUserInterface
{
    /// <summary>
    /// Show the page with [input] until the user answers it, the answer read as a
    /// <typeparamref name="TAnswer"/>. The task waits meanwhile, which is why the branch holds
    /// there, and the page can call its methods. Asked again, the page opens again with the new input.
    /// </summary>
    Task<TAnswer> AskAsync<TAnswer>(object? input, CancellationToken cancellation = default);
}

/// <summary>
/// A method of a user interface task its page calls while the task asks, on the very instance
/// running : <c>await window.fantoche.call('Name', args)</c>. It takes at most one value, read from
/// [args], and can take the <see cref="ITaskRuntime"/> and a <see cref="CancellationToken"/> along ;
/// what it returns is handed back to the page, the files and tables it holds being reachable from
/// the page from then on.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PageMethodAttribute : Attribute
{
}
