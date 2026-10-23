using System.Windows.Controls;

namespace Fantoche.App.Features.Storage
{
    public partial class StoragePage : UserControl
    {
        public StorageViewModel ViewModel => (StorageViewModel)this.DataContext;

        public StoragePage()
        {
            InitializeComponent();
            this.Loaded += (_, __) => _ = ViewModel.RefreshAsync();
        }
    }
}
