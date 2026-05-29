using Automation.App.Shared.ApiClients;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;

namespace Automation.App
{
    public class Services
    {
        private static readonly Lazy<IServiceProvider> lazy = new(() => ConfigureServices(new ServiceCollection()));
        public static IServiceProvider Provider => lazy.Value;

        private static IServiceProvider ConfigureServices(ServiceCollection services)
        {
            // Avalonia equivalent of WPF's DesignerProperties.GetIsInDesignMode.
            bool isInDesignMode = Design.IsDesignMode;

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: isInDesignMode)
                .Build();

            string apiUrl = isInDesignMode ? "http://design-time-placeholder" :
                config["ApiUrl"] ?? throw new Exception("Missing 'ApiUrl' in appsettings.json");

            string redisUrl = isInDesignMode ? "design-time-placeholder" :
                config["RedisConnectionString"] ?? throw new Exception("Missing 'RedisConnectionString' in appsettings.json");

            services.AddTransient<MainWindow>();

            // ShadUI overlay services: modal dialogs via DialogManager + <DialogHost>, transient
            // notifications via ToastManager + <ToastHost> (replaces the old Joufflu Modal/Alert).
            // Hosted in MainWindow (Phase 3); injected into view-models that raise dialogs/toasts.
            services.AddSingleton<ShadUI.DialogManager>();
            services.AddSingleton<ShadUI.ToastManager>();

            // TODO (Phase 4): re-register ParametersViewModel once it is ported to ShadUI theming.
            services.AddSingleton<RestClient>(new RestClient(apiUrl));
            services.AddSingleton<TaskProgressHubClient>(new TaskProgressHubClient(apiUrl));

            services.AddTransient<ScopesClient>(p => new ScopesClient(p.GetRequiredService<RestClient>()));
            services.AddTransient<TasksClient>(p => new TasksClient(p.GetRequiredService<RestClient>()));
            services.AddTransient<TaskInstancesClient>(p => new TaskInstancesClient(p.GetRequiredService<RestClient>()));
            services.AddTransient<PackagesClient>(p => new PackagesClient(p.GetRequiredService<RestClient>()));
            return services.BuildServiceProvider();
        }
    }

    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            // Handle exceptions from background threads. UI-thread exceptions are routed to the
            // in-app Alert overlay in Phase 3 once IWindowContainer is ported.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = Services.Provider.GetRequiredService<MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void UIThread_UnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // TODO (Phase 3): route to the current window's Alert overlay (IWindowContainer).
            // Prevent the application from crashing on UI-thread exceptions.
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            // The application will still terminate after this event for unhandled background exceptions.
        }
    }
}
