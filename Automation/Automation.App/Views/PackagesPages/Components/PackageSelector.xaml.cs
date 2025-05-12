using Automation.App.Shared.ApiClients;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace Automation.App.Views.PackagesPages.Components
{
    public class PackageSelectorModal : PackageSelector, IModalContent
    {
        public IModal? ParentLayout { get; set; }
        public ModalOptions Options => new ModalOptions()
        {
            Title = "Select package"
        };

        protected override void OnTargetSelected(PackageInfos package, PackageClass targetClass)
        {
            base.OnTargetSelected(package, targetClass);
            ParentLayout?.Hide(true);
        }
    }

    /// <summary>
    /// Logique d'interaction pour PackageSelector.xaml
    /// </summary>
    public partial class PackageSelector : UserControl, INotifyPropertyChanged
    {
        private readonly PackagesClient _packageClient;

        private IModal _modal => this.GetCurrentModalContainer();

        public ListPageWrapper<PackageInfos> Packages
        {
            get;
            private set;
        } = new ListPageWrapper<PackageInfos>() { PageSize = 50, Page = 1, Total = -1, };

        public PackageInfos? SelectedInfos { get; set; }
        public TargetedPackage? SelectedTarget { get; set; }
        public string SearchText { get; set; } = string.Empty;

        public PackageSelector()
        {
            _packageClient = Services.Provider.GetRequiredService<PackagesClient>();
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

        private async void ButtonAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var createPackage = new PackageCreateModal();
            if (await _modal.Show(createPackage) && createPackage.Package != null)
            {
                DisplayDetail(createPackage.Package);
            }
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null)
            {
                DisplayDetail((PackageInfos)item.DataContext);
            }
        }

        private async void DisplayDetail(PackageInfos package)
        {
            var modal = new PackageDetailModal(package);
            if (await _modal.Show(modal) &&  modal.SelectedClass != null)
            {
                OnTargetSelected(package, modal.SelectedClass);
            }
        }

        protected virtual void OnTargetSelected(PackageInfos package, PackageClass targetClass)
        {
            SelectedInfos = package;
            SelectedTarget = new TargetedPackage(SelectedInfos.Identifier, targetClass);
        }
    }
    #endregion
}
