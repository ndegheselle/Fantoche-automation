using Automation.App.Shared.ApiClients;
using Automation.Shared.Base;
using Automation.Shared.Packages;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace Automation.App.Views.PackagesPages.Components
{
    public class PackageSelectorModal : PackageSelector, IModalContentValidation
    {
        public Modal? ParentLayout { get; set; }

        public ModalValidationOptions Options => new ModalValidationOptions()
        {
            Title = "Select package",
            ValidButtonText = "Select"
        };

        protected override void OnPackageCreated(PackageInfos package)
        {
            SelectedPackage = package;
            ParentLayout?.Hide(true);
        }
    }

    /// <summary>
    /// Logique d'interaction pour PackageSelector.xaml
    /// </summary>
    public partial class PackageSelector : UserControl, INotifyPropertyChanged
    {
        private readonly App _app = (App)App.Current;
        private readonly PackagesClient _packageClient;

        private IModal _modal => this.GetCurrentModalContainer();

        public ListPageWrapper<PackageInfos> Packages
        {
            get;
            private set;
        } = new ListPageWrapper<PackageInfos>() { PageSize = 50, Page = 1, Total = -1, };

        public event EventHandler<PackageInfos?>? SelectedPackageChanged;
        public event EventHandler<PackageInfos>? PackageClicked;

        public PackageInfos? SelectedPackage { get; set; }

        public string SearchText { get; set; } = string.Empty;

        public PackageSelector()
        {
            _packageClient = _app.ServiceProvider.GetRequiredService<PackagesClient>();
            this.PropertyChanged += PackageSelector_PropertyChanged;
            InitializeComponent();

            Search("");
        }


        private async void Search(string search, int page = 1, int pageSize = 50)
        { Packages = await _packageClient.SearchAsync(search, page, pageSize); }

        #region UI events
        private void PackageSelector_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                Search(SearchText.Trim(), InstancesPaging.PageNumber, InstancesPaging.Capacity);
            }
        }

        private void InstancesPaging_PagingChange(int pageNumber, int capacity)
        { Search(SearchText.Trim(), pageNumber, capacity); }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { SelectedPackageChanged?.Invoke(this, SelectedPackage); }

        private async void ButtonAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var createPackage = new PackageCreateModal();
            if (await _modal.Show(createPackage) && createPackage.Package != null)
            {
                OnPackageCreated(createPackage.Package.Value);
            }
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null)
            {
                PackageClicked?.Invoke(this, (PackageInfos)item.DataContext);
            }
        }

        protected virtual void OnPackageCreated(PackageInfos package)
        {}
    }
    #endregion
}
