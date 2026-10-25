using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Worker.Storage;
using Fantoche.Worker.UserInterfaces;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// What the page of a user interface task reaches : the files of its folder and the storage its
/// input points at, nothing else. A page is shown on every machine its run is answered from.
/// </summary>
[TestFixture]
internal sealed class PageFilesTests
{
    private string _folder = null!;
    private string _page = null!;
    private LocalStorage _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _page = Path.Combine(_folder, "page");
        Directory.CreateDirectory(Path.Combine(_page, "scripts"));
        File.WriteAllText(Path.Combine(_page, "index.html"), """<html><head lang="en"><link rel="stylesheet" href="page.css"></head></html>""");
        File.WriteAllText(Path.Combine(_page, "scripts", "page.js"), "");
        File.WriteAllText(Path.Combine(_folder, "secret.txt"), "out of the page");
        _storage = new LocalStorage(Path.Combine(_folder, "storage"));
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_folder, true);

    private PageFiles FilesWith(object? input = null)
        => new(_page, _storage, input == null ? null : JToken.FromObject(input));

    private Task<FileRef> SaveFileAsync(string text)
        => _storage.SaveFileAsync("notes.txt", new MemoryStream(Encoding.UTF8.GetBytes(text)));

    private static async Task<string> TextOf(PageResponse response)
        => await new StreamReader(response.Content!).ReadToEndAsync();

    [Test]
    public async Task FileOfThePage_IsServed()
    {
        PageResponse response = await FilesWith().ReadAsync($"https://{PageFiles.Host}/scripts/page.js");

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(response.Headers, Does.Contain("Content-Type: text/javascript"));
        });
    }

    [Test]
    public async Task HtmlOfThePage_LinksPicoBeforeItsOwnStyles()
    {
        PageResponse response = await FilesWith().ReadAsync(PageFiles.Index.ToString());
        string html = await TextOf(response);

        Assert.That(html.IndexOf(PageFiles.PicoPath), Is.GreaterThan(0).And.LessThan(html.IndexOf("page.css")));
    }

    [Test]
    public async Task Pico_IsServed()
    {
        PageResponse response = await FilesWith().ReadAsync($"https://{PageFiles.Host}{PageFiles.PicoPath}");

        Assert.Multiple(async () =>
        {
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(await TextOf(response), Does.Contain("Pico CSS"));
        });
    }

    [Test]
    public async Task FileOutOfThePage_IsRefused()
    {
        // An encoded slash survives the parsing of the url, and only becomes "../" once unescaped.
        PageResponse response = await FilesWith().ReadAsync($"https://{PageFiles.Host}/..%2Fsecret.txt");

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(403));
            Assert.That(response.Content, Is.Null);
        });
    }

    [Test]
    public async Task OtherHost_IsRefused()
    {
        PageResponse response = await FilesWith().ReadAsync("https://example.com/index.html");

        Assert.That(response.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task Page_IsKeptOffTheNetwork()
    {
        PageResponse response = await FilesWith().ReadAsync(PageFiles.Index.ToString());

        Assert.That(response.Headers, Does.Contain("Content-Security-Policy: default-src 'self'"));
    }

    [Test]
    public async Task StoredFileOfTheInput_IsServed()
    {
        FileRef file = await SaveFileAsync("hello");

        PageResponse response = await FilesWith(new { Notes = file })
            .ReadAsync($"https://{PageFiles.Host}{PageFiles.StoragePath}files/{file.Id}/{file.Name}");

        Assert.That(await TextOf(response), Is.EqualTo("hello"));
    }

    [Test]
    public async Task StoredFileOutOfTheInput_IsRefused()
    {
        FileRef file = await SaveFileAsync("not handed over");

        PageResponse response = await FilesWith(new { Title = "nothing" })
            .ReadAsync($"https://{PageFiles.Host}{PageFiles.StoragePath}files/{file.Id}/{file.Name}");

        Assert.That(response.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task TableOfTheInput_IsReadASliceAtATime()
    {
        TableRef table = await _storage.CreateTableAsync("SELECT * FROM range(10) AS numbers(n)");

        PageResponse response = await FilesWith(new { Numbers = table })
            .ReadAsync($"https://{PageFiles.Host}{PageFiles.StoragePath}tables/{table.Id}?offset=4&limit=3");

        JObject page = JObject.Parse(await TextOf(response));
        Assert.Multiple(() =>
        {
            Assert.That(page["Total"]!.Value<long>(), Is.EqualTo(10));
            Assert.That(page["Rows"]!.Select(x => x["n"]!.Value<long>()), Is.EqualTo(new long[] { 4, 5, 6 }));
        });
    }
}
