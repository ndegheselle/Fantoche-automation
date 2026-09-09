using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Features.Packages
{
    public partial class PackagesPage : UserControl
    {
        public PackagesViewModel ViewModel => (PackagesViewModel)this.DataContext;

        public PackagesPage()
        {
            InitializeComponent();
            this.Loaded += (_, __) => _ = ViewModel.RefreshAsync();
        }

        private void SelectFile(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Packages files (*.nupkg;*.snupkg)|*.nupkg;*.snupkg"
            };
            if (openFileDialog.ShowDialog() != true)
                return;

            _ = ViewModel.AddPackageAsync(openFileDialog.FileName);
        }
    }
}
