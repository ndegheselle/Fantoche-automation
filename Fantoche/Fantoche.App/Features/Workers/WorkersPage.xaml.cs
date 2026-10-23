using System.Windows.Controls;

namespace Fantoche.App.Features.Workers
{
    public partial class WorkersPage : UserControl
    {
        public WorkersViewModel? ViewModel => DataContext as WorkersViewModel;

        /// <summary>
        /// The view model subscribed on load : the DataContext is already cleared when the page is
        /// unloaded, leaving the page.
        /// </summary>
        private WorkersViewModel? _subscribed;

        public WorkersPage()
        {
            InitializeComponent();
            this.Loaded += (_, __) =>
            {
                _subscribed = ViewModel;
                _ = _subscribed?.SubscribeAsync();
            };
            this.Unloaded += (_, __) =>
            {
                _subscribed?.Unsubscribe();
                _subscribed = null;
            };
        }
    }
}
