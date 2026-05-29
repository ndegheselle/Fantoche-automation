using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// Reusable breadcrumb control. Exposes a <see cref="Scope"/> input and a
    /// <see cref="ScopeSelected"/> output; data loading is delegated to its view-model.
    /// </summary>
    public partial class ScopedBreadcrumb : UserControl
    {
        public static readonly StyledProperty<Scope?> ScopeProperty =
            AvaloniaProperty.Register<ScopedBreadcrumb, Scope?>(nameof(Scope));

        /// <summary>The scope whose ancestor chain is displayed.</summary>
        public Scope? Scope
        {
            get => GetValue(ScopeProperty);
            set => SetValue(ScopeProperty, value);
        }

        /// <summary>Raised when the user selects a different scope in the breadcrumb.</summary>
        public event Action<Scope>? ScopeSelected;

        private readonly ScopedBreadcrumbViewModel _viewModel;

        public ScopedBreadcrumb()
        {
            // Controls are instantiated by the XAML loader, so the service is resolved from the
            // container rather than constructor-injected.
            _viewModel = new ScopedBreadcrumbViewModel(AppServices.Provider.GetRequiredService<IScopesService>());
            _viewModel.ScopeSelected += scope => ScopeSelected?.Invoke(scope);
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ScopeProperty)
                _ = _viewModel.LoadAsync(Scope);
        }

        private void ButtonScope_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Control { DataContext: Scope target })
                _viewModel.Select(target);
        }
    }
}
