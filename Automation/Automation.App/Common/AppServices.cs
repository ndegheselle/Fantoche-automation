using Automation.Shared.Services;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Automation.App.Common
{
    /// <summary>
    /// What the view models of a page reach, handed down from the shell rather than read off its
    /// singleton : what a view model uses is then written where it is built, and building one takes
    /// no application.
    /// </summary>
    public record AppServices(
        IScopedService Scoped,
        IExecutionService Execution,
        IHistoryService History,
        IPackagesService Packages,
        IOverlayService Overlays,
        IToastService Toasts);
}
