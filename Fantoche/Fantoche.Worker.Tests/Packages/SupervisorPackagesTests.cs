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
        _workerCache = new PackageCache(new SupervisorPackageSource(_http), Path.Combine(_folder, "worker"));
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
