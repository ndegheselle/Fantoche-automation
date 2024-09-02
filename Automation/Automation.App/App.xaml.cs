using Automation.App.Base;
using Automation.App.Components.Display;
using Automation.App.Shared.ApiClients;
using Automation.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using System.Windows;
using System.Windows.Threading;

namespace Automation.App
{
    public static class DependencyObjectExtension
    {
        public static IModalContainer GetCurrentModal(this DependencyObject d)
        {
            return ((IWindowContainer)Window.GetWindow(d)).Modal;
        }

        public static IAlert GetCurrentAlert(this DependencyObject d)
        {
            return ((IWindowContainer)Window.GetWindow(d)).Alert;
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Handle exceptions from the UI thread
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            // Handle exceptions from background threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

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
            services.AddSingleton<ParametersViewModel>();
            services.AddSingleton<RestClient>(new RestClient("https://localhost:8081/"));

            services.AddTransient<ScopeClient>(
                (provider) => new ScopeClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<TaskClient>(
                (provider) => new TaskClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<WorkflowClient>(
                (provider) => new WorkflowClient(provider.GetRequiredService<RestClient>()));
            services.AddTransient<HistoryClient>(
                (provider) => new HistoryClient(provider.GetRequiredService<RestClient>()));
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

            var modal = ServiceProvider.GetRequiredService<IModalContainer>();
            modal.Show(new ConfirmationModal("An unexpected error occurred")).Wait();

            // The application will still terminate after this event is handled
        }

        #endregion
    }
}
