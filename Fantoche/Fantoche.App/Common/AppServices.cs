using Fantoche.Shared.Services;
using Fantoche.Worker;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Fantoche.App.Common
{
    /// <summary>
    /// What the view models of a page reach, handed down from the shell rather than read off its
    /// singleton : what a view model uses is then written where it is built, and building one takes
    /// no application.
    /// </summary>
    /// <param name="Worker">The worker of the application, whose storage it reaches.</param>
    public record AppServices(
        ISupervisor Supervisor,
        LocalWorker Worker,
        IScopedService Scoped,
        IExecutionService Execution,
        IHistoryService History,
        IPackagesService Packages,
        IOverlayer Overlayer,
        IToastService Toasts);
}
