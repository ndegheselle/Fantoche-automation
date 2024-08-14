using Automation.App.Base;
using Automation.App.ViewModels;
using Automation.Supervisor.Client;
using Automation.Supervisor.Client.Test;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddTransient<IModalContainer>((provider) => GetActiveWindow().Modal);
            services.AddTransient<IAlert>((provider) => GetActiveWindow().Alert);

            services.AddSingleton<ParametersViewModel>();

            services.AddSingleton<ITaskClient>((provider) => new TestTaskClient());
            services.AddSingleton<IScopeClient>((provider) => new TestScopeClient());

            return services.BuildServiceProvider();
        }

        // XXX : should improve this if multiple windows are used
        private IWindowContainer GetActiveWindow()
        {
            return (IWindowContainer)Current.Windows.OfType<Window>().Single(x => x.IsActive);
        }
    }
}
