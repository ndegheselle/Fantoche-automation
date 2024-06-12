using Automation.App.Base;
using Automation.App.Contexts;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Automation.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            ServiceCollection services = new ServiceCollection();
            ServiceProvider = ConfigureServices(services);

            // Starting main window
            MainWindow mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        /// <summary>
        /// Register services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private IServiceProvider ConfigureServices(ServiceCollection services)
        {
            services.AddTransient<MainWindow>();
            services.AddTransient<IModalContainer>((provider) => GetActiveWindow()?.Modal);

            services.AddSingleton<SideMenuContext>();

            return services.BuildServiceProvider();
        }

        private IWindowContainer? GetActiveWindow()
        {
            return Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive) as IWindowContainer;
        }
    }
}
