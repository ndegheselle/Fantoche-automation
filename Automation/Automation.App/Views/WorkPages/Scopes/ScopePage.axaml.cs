using Automation.App.ViewModels.Scoped;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.WorkPages.Scopes
{
    public partial class ScopePage : UserControl
    {
        public ScopePage()
        {
            InitializeComponent();
            // Honour the element's requested focus (e.g. open settings right after creation).
            Loaded += (_, _) =>
            {
                if (DataContext is ScopePageViewModel { Scope.FocusOn: EnumScopedTab.Settings } vm)
                {
                    vm.Scope.FocusOn = EnumScopedTab.Default;
                    vm.OpenSettings();
                }
            };
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
