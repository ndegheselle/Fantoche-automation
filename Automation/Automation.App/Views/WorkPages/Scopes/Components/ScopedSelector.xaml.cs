using Automation.App.Shared.ApiClients;
using Automation.Dal.Models;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// Logique d'interaction pour ScopedSelector.xaml
    /// </summary>
    public partial class ScopedSelector : UserControl, INotifyPropertyChanged
    {
        public event Action<ScopedElement?>? SelectionChanged;

        #region Dependency properties
        public static readonly DependencyProperty CurrentScopeProperty =
            DependencyProperty.Register(
            nameof(CurrentScope),
            typeof(Scope),
            typeof(ScopedSelector),
            new PropertyMetadata(null));
        #endregion

        public Scope? CurrentScope
        {
            get { return (Scope)GetValue(CurrentScopeProperty); }
            set {
                SetValue(CurrentScopeProperty, value);
            }
        }

        private ScopedElement? _selected;
        public ScopedElement? Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                SelectionChanged?.Invoke(Selected);
            }
        }

        public ScopedElement? Current => Selected ?? CurrentScope;

        private readonly ScopesClient _client;

        public ScopedSelector()
        {
            _client = Services.Provider.GetRequiredService<ScopesClient>();
            this.Loaded += ScopedSelector_Loaded;
            InitializeComponent();
        }

        private async void ScopedSelector_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CurrentScope = await _client.GetRootAsync();
            CurrentScope.RefreshChildrens();
        }

        private async void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Selected is not Scope scope)
                return;
            CurrentScope = await _client.GetByIdAsync(scope.Id);
            CurrentScope!.RefreshChildrens();
        }

        private void ListBox_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Selected = null;
        }
    }
}