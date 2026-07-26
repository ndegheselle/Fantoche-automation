using Automation.App.Features.History;
using Automation.App.Features.Home;
using Automation.App.Features.Packages;
using Automation.App.Features.Workers;
using Automation.App.Features.Workflows;
using Automation.Services.Local;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Controls;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;

namespace Automation.App
{
    internal class ShellViewModel : ObservableObject
    {
        public Navigator Navigator { get; } = new();
        public OverlayService Overlays { get; } = new();
        public ToastService Toasts { get; } = new();

        private readonly Dictionary<string, object> _pages;
        public Func<string, object?> ResolveTarget { get; }

        public ShellViewModel()
        {
            // Compose the local (in-memory) services. They mock the supervisor backend so the app
            // can be run and demonstrated without a redis / mongo server.
            IHistoryService history = new LocalHistoryService();
            IScopedService scoped = new LocalScopedService(history);
            IWorkersService workers = new LocalWorkersService();
            IPackagesService packages = new LocalPackagesService(
                Path.Combine(AppContext.BaseDirectory, "packages-source"));

            _pages = new()
            {
                ["home"] = new HomeViewModel(),
                ["workflows"] = new WorkflowsViewModel(scoped),
                ["history"] = new HistoryViewModel(history),
                ["packages"] = new PackagesViewModel(packages, Toasts),
                ["workers"] = new WorkersViewModel(workers),
            };

            ResolveTarget = Resolve;

            // Land on a page at startup.
            Navigator.Navigate(_pages["home"]);
        }

        object? Resolve(string target)
        {
            return _pages.GetValueOrDefault(target);
        }
    }
}
