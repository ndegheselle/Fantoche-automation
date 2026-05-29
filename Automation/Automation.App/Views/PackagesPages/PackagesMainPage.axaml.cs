using Automation.Shared.Data.Execution;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.PackagesPages
{
    public partial class PackagesMainPage : UserControl
    {
        public PackagesMainPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private PackagesMainPageViewModel? ViewModel => DataContext as PackagesMainPageViewModel;

        private void PackageList_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is ListBox { SelectedItem: PackageInfos package })
                ViewModel?.OpenDetail(package);
        }

        private void Paging_PagingChange(int pageNumber, int capacity)
        {
            _ = ViewModel?.SearchAsync(pageNumber, capacity);
        }
    }
}
