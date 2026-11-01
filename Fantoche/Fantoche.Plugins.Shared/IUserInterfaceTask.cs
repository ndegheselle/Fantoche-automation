namespace Fantoche.Plugins.Shared;

/// <summary>
/// A page asking the user for the parameters of another task, before that task runs : in a
/// workflow it is attached under the task, which runs unattended without it. Such a page always
/// runs in an application, where the user is : a worker reaching it hands it to one through the
/// supervisor. Derive from <see cref="UserInterfaceTask{TParameters}"/> rather than implementing it.
/// <para>
/// The page reads the parameters it edits as <c>window.fantoche.input</c> and answers them with
/// <c>await window.fantoche.answer(value)</c>, rejected when the answer doesn't fit what the task
/// expects. Meanwhile it can call the methods of the page task marked with
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
/// The page editing the <typeparamref name="TParameters"/> of a task : it hands over what the user
/// answered, which the task then runs with. The page methods it declares usually call static
/// methods of the task, so the page applies the rules of the task rather than a copy of them.
/// </summary>
public abstract class UserInterfaceTask<TParameters> : BaseTask<TParameters, TParameters>, IUserInterfaceTask
{
    public abstract string PageFolder { get; }

    public sealed override async Task<TParameters> DoAsync(TParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        IUserInterface page = runtime.UserInterface
            ?? throw new InvalidOperationException("A page task runs where its page can be shown.");
        return await page.AskAsync<TParameters>(parameters, cancellation ?? CancellationToken.None);
    }
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
/// A method of a page task its page calls while it asks, on the very instance running :
/// <c>await window.fantoche.call('Name', args)</c>. It takes at most one value, read from [args],
/// and can take the <see cref="ITaskRuntime"/> and a <see cref="CancellationToken"/> along ; what it
/// returns is handed back to the page, the files and tables it holds being reachable from the page
/// from then on.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PageMethodAttribute : Attribute
{
}
