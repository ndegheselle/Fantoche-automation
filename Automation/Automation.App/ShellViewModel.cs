using Automation.App.Features.Home;
using Automation.App.Features.Packages;
using Automation.App.Features.Servers;
using Automation.App.Features.Storage;
using Automation.App.Features.Workflows;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Feedback.Controls;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;

namespace Automation.App
{
    public class ShellViewModel : ObservableObject
    {
        public Navigator Navigator { get; }
        public OverlayService Overlays { get; } = new();
        public ToastService Toasts { get; } = new();

        private readonly Dictionary<Type, object> _pages;

        public ShellViewModel()
        {
            _pages = new object[]
            {
                new HomeViewModel(),
                new WorkflowsViewModel(),
                new PackagesViewModel(),
                new ServersViewModel(),
                new StorageViewModel(),
            }.ToDictionary(x => x.GetType());

            Navigator = new Navigator(Resolve);
            // Land on a page at startup.
            Navigator.Navigate(typeof(HomeViewModel));
        }

        object? Resolve(Type target)
        {
            return _pages.GetValueOrDefault(target);
        }
    }
}
