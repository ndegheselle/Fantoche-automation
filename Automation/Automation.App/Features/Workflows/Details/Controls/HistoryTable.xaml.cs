using System.Windows.Controls;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Displays the executions of the <see cref="HistoryViewModel"/> set as its DataContext, following
    /// them live : it subscribes to the history while it is displayed and unsubscribes as soon as it
    /// isn't (e.g. when another tab is selected).
    /// </summary>
    public partial class HistoryTable : UserControl
    {
        public HistoryViewModel? ViewModel => DataContext as HistoryViewModel;

        public HistoryTable()
        {
            InitializeComponent();
            Loaded += (_, _) => _ = ViewModel?.SubscribeAsync();
            Unloaded += (_, _) => ViewModel?.Unsubscribe();
        }
    }
}
