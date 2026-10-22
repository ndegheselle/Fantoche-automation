using Fantoche.Worker.Packages;

namespace Fantoche.Worker.Tests.Packages;

[TestFixture]
internal sealed class PackageCacheTests
{
    private string _folder = null!;
    private PackageFolder _packages = null!;
    private PackageCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _packages = new PackageFolder(Path.Combine(_folder, "nuget"));
        _cache = new PackageCache(_packages, Path.Combine(_folder, "cache"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Test]
    public async Task Download_ConcurrentRequestsForTheSamePackageAllSucceed()
    {
        await _packages.AddAsync(TestNupkg.Write(Path.Combine(_folder, "upload"), "Tests.Package", "1.0.0"));

        // A worker runs several executions at once, which may all need the same new package.
        string[] paths = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => _cache.DownloadPackageAsync("Tests.Package", new Version(1, 0, 0), TestNupkg.Dll))));

        Assert.Multiple(() =>
        {
            Assert.That(paths.Distinct().Count(), Is.EqualTo(1));
            Assert.That(File.Exists(paths[0]), Is.True);
        });
    }

}
