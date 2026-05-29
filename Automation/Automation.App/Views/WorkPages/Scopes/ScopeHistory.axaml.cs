using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace Automation.App.Views.WorkPages.Scopes
{
    /// <summary>
    /// Execution-history control for a scope. Takes a <see cref="Scope"/> input; data loading and
    /// paging are delegated to <see cref="ScopeHistoryViewModel"/>.
    /// </summary>
    public partial class ScopeHistory : UserControl
    {
        public static readonly StyledProperty<Scope?> ScopeProperty =
            AvaloniaProperty.Register<ScopeHistory, Scope?>(nameof(Scope));

        public Scope? Scope
        {
            get => GetValue(ScopeProperty);
            set => SetValue(ScopeProperty, value);
        }

        private readonly ScopeHistoryViewModel _viewModel;

        public ScopeHistory()
        {
            _viewModel = new ScopeHistoryViewModel(AppServices.Provider.GetRequiredService<IScopesService>());
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ScopeProperty && Scope is not null)
            {
                _viewModel.ScopeId = Scope.Id;
                _ = _viewModel.LoadAsync(_viewModel.Page, _viewModel.PageSize);
            }
        }

        private void Paging_PagingChange(int pageNumber, int capacity)
        {
            _ = _viewModel.LoadAsync(pageNumber, capacity);
        }
    }
}
