using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Worker.Storage;

namespace Fantoche.Worker.Tests;

[TestFixture]
internal sealed class LocalStorageTests
{
    private string _folder = null!;
    private LocalStorage _storage = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));
        _storage = new LocalStorage(_folder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    private Task<TableRef> PeopleAsync()
        => _storage.CreateTableAsync("SELECT * FROM (VALUES ('alice', 10), ('bob', 20), ('carol', 30)) AS people(name, amount)");

    private static async Task<List<IReadOnlyDictionary<string, object?>>> ToListAsync(IAsyncEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        List<IReadOnlyDictionary<string, object?>> list = [];
        await foreach (var row in rows)
            list.Add(row);
        return list;
    }

    [Test]
    public async Task CreateTable_ReportsTheColumnsOfTheResult()
    {
        TableRef table = await PeopleAsync();

        Assert.That(table.Columns.Select(x => (x.Name, x.Type)),
            Is.EqualTo(new[] { ("name", "VARCHAR"), ("amount", "INTEGER") }));
    }

    [Test]
    public async Task ReadRows_KeepsTheOrderOfTheQuery()
    {
        TableRef table = await PeopleAsync();

        var rows = await ToListAsync(_storage.ReadRowsAsync(table));

        Assert.That(rows.Select(x => x["name"]), Is.EqualTo(new[] { "alice", "bob", "carol" }));
    }

    [Test]
    public async Task CreateTable_ReadsOtherTablesById()
    {
        TableRef source = await PeopleAsync();

        TableRef filtered = await _storage.CreateTableAsync(
            $"SELECT upper(name) AS name FROM {source.Id} WHERE amount >= $min ORDER BY amount",
            new Dictionary<string, object?> { ["min"] = 20 });
        var rows = await ToListAsync(_storage.ReadRowsAsync(filtered));

        Assert.Multiple(() =>
        {
            Assert.That(filtered.Id, Is.Not.EqualTo(source.Id));
            Assert.That(filtered.Columns.Select(x => x.Name), Is.EqualTo(new[] { "name" }));
            Assert.That(rows.Select(x => x["name"]), Is.EqualTo(new[] { "BOB", "CAROL" }));
        });
    }

    [Test]
    public async Task Table_IsReadByAnotherStorageOnTheSameFolder()
    {
        // Two storages on one folder : two machines sharing it.
        LocalStorage other = new(_folder);
        TableRef source = await PeopleAsync();

        TableRef filtered = await other.CreateTableAsync($"SELECT name FROM {source.Id} WHERE amount > 15");
        var rows = await ToListAsync(_storage.ReadRowsAsync(filtered));

        Assert.That(rows.Select(x => x["name"]), Is.EqualTo(new[] { "bob", "carol" }));
    }

    [Test]
    public async Task ReadRows_RefusesATableItDidNotName()
    {
        var table = new TableRef { Id = "t_x; DROP TABLE y" };

        Assert.ThrowsAsync<ArgumentException>(async () => await ToListAsync(_storage.ReadRowsAsync(table)));
    }

    [Test]
    public async Task SavedFile_IsOpenedWithItsHandle()
    {
        FileRef file = await _storage.SaveFileAsync("report.pdf", new MemoryStream(Encoding.UTF8.GetBytes("content")));

        await using Stream stream = await _storage.OpenFileAsync(file);
        using var reader = new StreamReader(stream);

        Assert.Multiple(async () =>
        {
            Assert.That(file.Name, Is.EqualTo("report.pdf"));
            Assert.That(await reader.ReadToEndAsync(), Is.EqualTo("content"));
        });
    }

    [TestCase("../outside.txt")]
    [TestCase("..")]
    [TestCase("")]
    public void OpenFile_RefusesANameLeavingItsFolder(string name)
    {
        var file = new FileRef { Id = Guid.NewGuid().ToString("N"), Name = name };

        Assert.ThrowsAsync<ArgumentException>(() => _storage.OpenFileAsync(file));
    }
}
