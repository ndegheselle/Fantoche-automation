using Fantoche.Plugins.Shared;
using Fantoche.Worker.UserInterfaces;

namespace Fantoche.Worker.Executor;

/// <summary>
/// Where the tasks asking the user run (see <see cref="IUserInterfaceTask"/>) : always in an
/// application, where the user is. An application runs them itself, showing their page ; a worker
/// hands them to an application through the supervisor, as it would to a remote worker.
/// </summary>
public sealed class UserTasks
{
    /// <summary>What shows their page, in an application.</summary>
    public IPageHost? Pages { get; }

    /// <summary>What runs them in an application, from a worker.</summary>
    public ITaskExecutor? Applications { get; }

    /// <summary>Neither : a task asking the user fails, nothing being there to show its page.</summary>
    public static readonly UserTasks None = new(null, null);

    private UserTasks(IPageHost? pages, ITaskExecutor? applications)
    {
        Pages = pages;
        Applications = applications;
    }

    public static UserTasks ShownBy(IPageHost pages) => new(pages, null);

    public static UserTasks HandedTo(ITaskExecutor applications) => new(null, applications);
}
