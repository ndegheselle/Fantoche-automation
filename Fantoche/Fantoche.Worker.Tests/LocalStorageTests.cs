using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Worker.Storage;
using Newtonsoft.Json.Linq;

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
    public async Task ReadRows_RefusesATableItDidNotName()
    {
        var table = new TableRef { Id = "t_x; DROP TABLE y" };

        await Assert.ThrowsAsync<ArgumentException>(async () => await ToListAsync(_storage.ReadRowsAsync(table)));
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
    public async Task OpenFile_RefusesANameLeavingItsFolder(string name)
    {
        var file = new FileRef { Id = $"f_{Guid.NewGuid():N}", Name = name };

        await Assert.ThrowsAsync<ArgumentException>(() => _storage.OpenFileAsync(file));
    }

    [Test]
    public async Task IdsIn_FindsTheHandlesWhereverTheyAre()
    {
        TableRef table = await PeopleAsync();
        FileRef file = await _storage.SaveFileAsync("report.pdf", new MemoryStream(Encoding.UTF8.GetBytes("content")));
        JToken output = JToken.FromObject(new { Source = table, Report = new { Files = new[] { file } }, Name = "t_not-an-id" });

        Assert.That(LocalStorage.IdsIn(output), Is.EquivalentTo(new[] { table.Id, file.Id }));
    }

    [Test]
    public async Task ImportedItem_IsReadWithTheHandleOfTheStorageItCameFrom()
    {
        TableRef table = await PeopleAsync();
        LocalStorage other = new(Path.Combine(_folder, "other"));

        foreach (string item in _storage.ItemsOf(table.Id))
        {
            await using Stream content = _storage.OpenItem(item);
            await other.ImportItemAsync(item, content);
        }
        var rows = await ToListAsync(other.ReadRowsAsync(table));

        Assert.That(rows.Select(x => x["name"]), Is.EqualTo(new[] { "alice", "bob", "carol" }));
    }

    [TestCase("../outside.parquet")]
    [TestCase("tables/../../outside.parquet")]
    [TestCase("files/f_0123456789abcdef0123456789abcdef/..")]
    [TestCase("database.db")]
    public async Task ImportItem_RefusesAPathItWouldNotName(string item)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.ImportItemAsync(item, new MemoryStream()));
    }
}
