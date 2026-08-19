using Automation.Shared.Base;
using Microsoft.Win32;
using System.Windows.Controls;

namespace Automation.App.Features.Packages
{
    /// <summary>
    /// Logique d'interaction pour PackagesPage.xaml
    /// </summary>
    public partial class PackagesPage : UserControl
    {
        public PackagesViewModel ViewModel => (PackagesViewModel)this.DataContext;

        public PackagesPage()
        {
            InitializeComponent();
            this.Loaded += (_, __) => _ = ViewModel.RefreshAsync();
        }

        #region UI events
        private void SelectFile(object sender, System.Windows.RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Packages files (*.nupkg)|*.nupkg"
            };
            if (openFileDialog.ShowDialog() != true)
                return;

            ViewModel.AddPackage(openFileDialog.FileName);
        }
        #endregion
    }
}
