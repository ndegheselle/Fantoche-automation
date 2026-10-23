using System.Windows.Controls;

namespace Fantoche.App.Features.Workers
{
    public partial class WorkersPage : UserControl
    {
        public WorkersViewModel ViewModel => (WorkersViewModel)this.DataContext;

        public WorkersPage()
        {
            InitializeComponent();
            this.Loaded += (_, __) => _ = ViewModel.SubscribeAsync();
            this.Unloaded += (_, __) => ViewModel.Unsubscribe();
        }
    }
}
