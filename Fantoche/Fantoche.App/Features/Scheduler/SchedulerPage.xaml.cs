using System.Windows.Controls;

namespace Fantoche.App.Features.Scheduler
{
    public partial class SchedulerPage : UserControl
    {
        public SchedulerViewModel? ViewModel => DataContext as SchedulerViewModel;

        public SchedulerPage()
        {
            InitializeComponent();
            // Read each time it is shown : the schedules are edited on another page.
            this.Loaded += (_, __) => _ = ViewModel?.RefreshAsync();
        }
    }
}
