using Automation.App.Shared.ApiClients;
using Automation.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using System.Windows;
using System.Windows.Threading;
using AdonisUI.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace Automation.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static new App Current => (App)Application.Current;

        private IServiceProvider? _serviceProvider;
        public IServiceProvider ServiceProvider
        {
            get {
                if (_serviceProvider == null)
                    _serviceProvider = ConfigureServices(new ServiceCollection());
                return _serviceProvider;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Handle exceptions from the UI thread
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            // Handle exceptions from background threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Starting main window
            MainWindow mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        /// <summary>
        /// Register services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceProvider ConfigureServices(ServiceCollection services)
        {
            services.AddTransient<MainWindow>();
            services.AddSingleton<ParametersViewModel>();
            services.AddSingleton<RestClient>(new RestClient("https://localhost:8081/"));

            services.AddTransient<ScopesClient>(
                (provider) => new ScopesClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<TasksClient>((provider) => new TasksClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<TaskInstancesClient>(
                (provider) => new TaskInstancesClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<PackagesClient>(
                (provider) => new PackagesClient(provider.GetRequiredService<RestClient>()));
            return services.BuildServiceProvider();
        }

        #region Exception handling
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // FIXME : Should get the current window where the exception happend
            var alert = ((IWindowContainer)Current.MainWindow).Alert;
            alert.Error(e.Exception.Message);
            // Prevent the application from crashing
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? exception = e.ExceptionObject as Exception;

            if (ServiceProvider == null)
                return;

            MessageBox.Show(
                "An unexpected error occurred",
                "Error",
                AdonisUI.Controls.MessageBoxButton.OK,
                AdonisUI.Controls.MessageBoxImage.Warning);
            // The application will still terminate after this event is handled
        }
        #endregion
    }
}
