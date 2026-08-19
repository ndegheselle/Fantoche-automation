using Automation.Shared.Base;
using Microsoft.Win32;
using System.IO;
using System.Linq.Expressions;
using System.Windows.Controls;

namespace Automation.App.Features.Packages
{
    /// <summary>
    /// Logique d'interaction pour PackagesPage.xaml
    /// </summary>
    public partial class PackagesPage : UserControl
    {
        public PackagesViewModel ViewModel => (PackagesViewModel)this.DataContext;

        private string _search = "";
        private PaginationOptions _paginationOptions = new PaginationOptions();

        public PackagesPage()
        {
            InitializeComponent();
        }

        #region UI events
        private void Search_SearchChanged(string search)
        {
            _search = search;
            ViewModel.Search(_search, _paginationOptions);
        }

        private void Paging_PagingChange(int pageNumber, int capacity)
        {
            _paginationOptions = new PaginationOptions() { Page = pageNumber, PageSize = capacity };
            ViewModel.Search(_search, _paginationOptions);
        }

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
