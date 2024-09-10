using Automation.App.Shared.ApiClients;
using Automation.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// Logique d'interaction pour PackageSelector.xaml
    /// </summary>
    public partial class PackageSelector : UserControl
    {
        private readonly App _app = (App)App.Current;
        private readonly PackagesClient _packageClient;

        public IEnumerable<PackagesClient> Packages { get; private set; } = new List<PackagesClient>();

        public PackageSelector()
        {
            _packageClient = _app.ServiceProvider.GetRequiredService<PackagesClient>();
            InitializeComponent();
        }

        private async void Search(string search, int page, int pageSize = 50)
        {

        }
    }
}
