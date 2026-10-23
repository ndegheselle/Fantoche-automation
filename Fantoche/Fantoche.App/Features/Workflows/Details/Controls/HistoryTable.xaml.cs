using System.Windows.Controls;

namespace Fantoche.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Displays the executions of the <see cref="HistoryViewModel"/> set as its DataContext, following
    /// them live : it subscribes to the history while it is displayed and unsubscribes as soon as it
    /// isn't (e.g. when another tab is selected).
    /// </summary>
    public partial class HistoryTable : UserControl
    {
        public HistoryViewModel? ViewModel => DataContext as HistoryViewModel;

        /// <summary>
        /// The view model subscribed on load : the DataContext can already be cleared when the table
        /// is unloaded (another element opened).
        /// </summary>
        private HistoryViewModel? _subscribed;

        public HistoryTable()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                _subscribed = ViewModel;
                _ = _subscribed?.SubscribeAsync();
            };
            Unloaded += (_, _) =>
            {
                _subscribed?.Unsubscribe();
                _subscribed = null;
            };
        }
    }
}
