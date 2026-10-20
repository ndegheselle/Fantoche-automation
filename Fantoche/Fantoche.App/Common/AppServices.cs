using Fantoche.Services.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker.Storage;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Fantoche.App.Common
{
    /// <summary>
    /// What the view models of a page reach, handed down from the shell rather than read off its
    /// singleton : what a view model uses is then written where it is built, and building one takes
    /// no application.
    /// </summary>
    /// <param name="Storage">The folder of the storage, the one the tasks run in the application use.</param>
    /// <param name="Workers">The workers of the supervisor, null in standalone mode.</param>
    public record AppServices(
        IScopedService Scoped,
        IExecutionService Execution,
        IHistoryService History,
        IPackagesService Packages,
        StorageFolder Storage,
        RemoteWorkersService? Workers,
        IOverlayer Overlayer,
        IToastService Toasts);
}
