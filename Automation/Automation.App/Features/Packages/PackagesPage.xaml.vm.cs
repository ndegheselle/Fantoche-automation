using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Packages
{
    public class PackagesViewModel : ObservableObject
    {
        private readonly IPackagesService packages;

        public PackagesViewModel(IPackagesService packages)
        {
            this.packages = packages;
        }
    }
}
