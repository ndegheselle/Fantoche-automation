using System.IO;
using Automation.App.Features.Home;
using Automation.App.Features.Packages;
using Automation.App.Features.Servers;
using Automation.App.Features.Storage;
using Automation.App.Features.Workflows;
using Automation.Services.Local;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Feedback.Controls;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;

namespace Automation.App;

public class Settings
{
    public const string ApplicationName = "Automation";

    public string PackagesFolderPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "nuggets");
    public string LocalFolderPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), ApplicationName);
}

public class SpineViewModel : ObservableObject
{
    #region Singleton
    private static readonly Lazy<SpineViewModel> lazy =
        new Lazy<SpineViewModel>(() => new SpineViewModel());

    public static SpineViewModel Instance { get { return lazy.Value; } }
    #endregion

    #region UI
    public Navigator Navigator { get; }
    public OverlayService Overlays { get; } = new();
    public ToastService Toasts { get; } = new();
    #endregion

    #region services
    public Settings Settings { get; } = new Settings();
    public IPackagesService Packages { get; }
    public IHistoryService History { get; }
    public IScopedService Scoped { get; }
    #endregion

    private readonly Dictionary<Type, object> _pages;

    private SpineViewModel()
    {
        var dbContextFactory = new LocalDbContextFactory(Path.Combine(Settings.LocalFolderPath, "automation.db"));

        Packages = new LocalPackagesService(Settings.PackagesFolderPath, Settings.LocalFolderPath);
        History = new LocalHistoryService(dbContextFactory);
        Scoped = new LocalScopedService(History, dbContextFactory);
        // Entity Framework's first query pays for the model building and the query compilation :
        // done here, in the background, the pages don't have to.
        _ = dbContextFactory.WarmupAsync();

        _pages = new object[]
        {
            new HomeViewModel(),
            new WorkflowsViewModel(Scoped),
            new PackagesViewModel(Packages, Overlays),
            new ServersViewModel(),
            new StorageViewModel(),
        }.ToDictionary(x => x.GetType());

        Navigator = new Navigator(Resolve);
        // Land on a page at startup.
        Navigator.Navigate(typeof(HomeViewModel));
    }

    object? Resolve(Type target)
    {
        return _pages.GetValueOrDefault(target);
    }
}
