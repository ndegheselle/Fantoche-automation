using Fantoche.Worker.UserInterfaces;

namespace Fantoche.Worker.Tests;

/// <summary>
/// What the page of a user interface task reaches : the files of its folder, nothing else. A page
/// is shown on every machine its run is answered from.
/// </summary>
[TestFixture]
internal sealed class PageFilesTests
{
    private string _folder = null!;
    private PageFiles _files = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        string page = Path.Combine(_folder, "page");
        Directory.CreateDirectory(Path.Combine(page, "scripts"));
        File.WriteAllText(Path.Combine(page, "index.html"), "<html></html>");
        File.WriteAllText(Path.Combine(page, "scripts", "page.js"), "");
        File.WriteAllText(Path.Combine(_folder, "secret.txt"), "out of the page");
        _files = new PageFiles(page);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_folder, true);

    [Test]
    public void FileOfThePage_IsServed()
    {
        PageResponse response = _files.Read($"https://{PageFiles.Host}/scripts/page.js");

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(response.Headers, Does.Contain("Content-Type: text/javascript"));
        });
    }

    [Test]
    public void FileOutOfThePage_IsRefused()
    {
        // An encoded slash survives the parsing of the url, and only becomes "../" once unescaped.
        PageResponse response = _files.Read($"https://{PageFiles.Host}/..%2Fsecret.txt");

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(403));
            Assert.That(response.Content, Is.Null);
        });
    }

    [Test]
    public void OtherHost_IsRefused()
    {
        PageResponse response = _files.Read("https://example.com/index.html");

        Assert.That(response.Status, Is.EqualTo(403));
    }

    [Test]
    public void Page_IsKeptOffTheNetwork()
    {
        PageResponse response = _files.Read(PageFiles.Index.ToString());

        Assert.That(response.Headers, Does.Contain("Content-Security-Policy: default-src 'self'"));
    }
}
