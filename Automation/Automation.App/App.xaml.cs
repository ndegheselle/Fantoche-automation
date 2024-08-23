using Automation.App.Base;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.App.ViewModels;
using Automation.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
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
            services.AddSingleton<HttpClient>(
                (provider) =>
                {
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri("https://localhost:50568/");
                    return client;
                });

            services.AddTransient<ScopeClient>(
                (provider) => new ScopeClient(provider.GetRequiredService<HttpClient>()));
            services.AddTransient<TaskClient>(
                (provider) => new TaskClient(provider.GetRequiredService<HttpClient>()));
            services.AddTransient<WorkflowClient>(
                (provider) => new WorkflowClient(provider.GetRequiredService<HttpClient>()));

            return services.BuildServiceProvider();
        }

        // XXX : should improve this if multiple windows are used
        private IWindowContainer GetActiveWindow()
        { return (IWindowContainer)Current.Windows.OfType<Window>().Single(x => x.IsActive); }
    }
}
