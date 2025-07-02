using Automation.App.Shared.ApiClients;
using Automation.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace Automation.App
{
    public class Services
    {
        private static readonly Lazy<IServiceProvider> lazy = new Lazy<IServiceProvider>(() => ConfigureServices(new ServiceCollection()));
        public static IServiceProvider Provider { get { return lazy.Value; } }

        /// <summary>
        /// Register services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceProvider ConfigureServices(ServiceCollection services)
        {
            // Check if running in design mode
            bool isInDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());

            string apiUrl = isInDesignMode ? "http://design-time-placeholder" :
                ConfigurationManager.AppSettings["ApiUrl"] ??
                throw new Exception("Missing 'ApiUrl' in App.Config");

            string redisUrl = isInDesignMode ? "design-time-placeholder" :
                ConfigurationManager.AppSettings["RedisConnectionString"] ??
                throw new Exception("Missing 'RedisConnectionString' in App.Config");

            services.AddTransient<MainWindow>();
            services.AddSingleton<ParametersViewModel>();
            services.AddSingleton<RestClient>(new RestClient(apiUrl));
            services.AddSingleton<TaskProgressHubClient>(new TaskProgressHubClient(apiUrl));

            services.AddTransient<ScopesClient>(
                (provider) => new ScopesClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<TasksClient>((provider) => new TasksClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<TaskInstancesClient>(
                (provider) => new TaskInstancesClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<PackagesClient>(
                (provider) => new PackagesClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<GraphsClient>(
                (provider) => new GraphsClient(provider.GetRequiredService<RestClient>()));
            return services.BuildServiceProvider();
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Handle exceptions from the UI thread
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            // Handle exceptions from background threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Starting main window
            MainWindow mainWindow = Services.Provider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        #region Exception handling
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (Current.MainWindow == null)
                return;

            // FIXME : Should get the current window where the exception happend
            var alert = ((IWindowContainer)Current.MainWindow).Alert;
            alert.Error(e.Exception.Message);
            // Prevent the application from crashing
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? exception = e.ExceptionObject as Exception;

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
