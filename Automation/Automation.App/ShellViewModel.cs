using Automation.App.Features.Home;
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
            _pages = new()
            {
                ["home"] = new HomeViewModel(),
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
