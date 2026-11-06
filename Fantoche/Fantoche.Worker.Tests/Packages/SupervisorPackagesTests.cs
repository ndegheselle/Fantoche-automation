using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using Fantoche.Services.Remote;
using Fantoche.Supervisor.Api;
using Fantoche.Worker.Packages;
using Microsoft.AspNetCore.Builder;

namespace Fantoche.Worker.Tests.Packages;

/// <summary>
/// The packages added to a supervisor, as a worker downloads them : the real supervisor, over the
/// loopback.
/// </summary>
[TestFixture]
internal sealed class SupervisorPackagesTests
{
    private static readonly Version TestVersion = new(1, 0, 0);

    private string _folder = null!;
    private WebApplication _supervisor = null!;
    private HttpClient _http = null!;
    private RemotePackagesService _packages = null!;
    private PackageCache _workerCache = null!;

    [SetUp]
    public async Task SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _supervisor = SupervisorApp.Build([
            "--urls", "http://127.0.0.1:0",
            $"--DATA_FOLDER={Path.Combine(_folder, "supervisor")}",
            "--Logging:LogLevel:Default=Warning"]);
        await _supervisor.StartAsync();

        _http = new HttpClient { BaseAddress = new Uri($"{_supervisor.Urls.First()}/") };
        _packages = new RemotePackagesService(_http);
        _workerCache = new PackageCache(new RemotePackageSource(_http), Path.Combine(_folder, "worker"));
    }

    [TearDown]
    public async Task TearDown()
    {
        _http.Dispose();
        await _supervisor.DisposeAsync();
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    private Task AddAsync(string id) => _packages.AddAsync(TestNupkg.Write(Path.Combine(_folder, "upload"), id, TestVersion.ToString()));

    /// <summary>A package of interfaces : built for Windows, as a WPF project is.</summary>
    private Task<PackageAdded> AddInterfacesAsync(string id)
        => _packages.AddAsync(TestNupkg.Write(Path.Combine(_folder, "upload"), id, TestVersion.ToString(), "net10.0-windows7.0"));

    [Test]
    public async Task PackageOfInterfaces_IsAddedWithoutItsTasksBeingLookedFor()
    {
        // The supervisor may run on Linux, where WPF can't be loaded : only the application lists
        // the interfaces.
        PackageAdded added = await AddInterfacesAsync("Tests.Interfaces");
        List<ClassTarget> classes = await _packages.GetClassesAsync("Tests.Interfaces", TestVersion);

        Assert.Multiple(() =>
        {
            Assert.That(added.Warnings, Is.Empty);
            Assert.That(classes, Is.Empty);
        });
    }

    [Test]
    [Platform(Include = "Win")]
    public async Task PackageOfInterfaces_IsExtractedForTheApplication()
    {
        await AddInterfacesAsync("Tests.Interfaces");
        PackageCache applicationCache = new(new RemotePackageSource(_http), Path.Combine(_folder, "interfaces"), framework: PackageCache.WindowsFramework);

        string path = await applicationCache.DownloadPackageAsync("Tests.Interfaces", TestVersion, TestNupkg.Dll);

        Assert.That(File.Exists(path), Is.True);
    }

    [Test]
    public async Task Download_ExtractsThePackageTheSupervisorHolds()
    {
        await AddAsync("Tests.Package");

        string path = await _workerCache.DownloadPackageAsync("Tests.Package", TestVersion, TestNupkg.Dll);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(path), Is.True);
            Assert.That(path, Does.StartWith(Path.Combine(_folder, "worker")));
        });
    }

    [Test]
    public async Task Download_FailsForAPackageTheSupervisorDoesNotHold()
    {
        await AddAsync("Tests.Package");

        await Assert.ThrowsAsync<PackageDownloadException>(() => _workerCache.DownloadPackageAsync("Tests.Unknown", TestVersion));
    }

    [Test]
    public async Task Add_RefusesAVersionTheSupervisorAlreadyHolds()
    {
        await AddAsync("Tests.Package");

        // The workers would keep the assemblies of the first one.
        await Assert.ThrowsAsync<InvalidOperationException>(() => AddAsync("Tests.Package"));
    }
}
