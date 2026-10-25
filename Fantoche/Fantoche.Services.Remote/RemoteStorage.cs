using System.Globalization;
using System.Runtime.CompilerServices;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Remote;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Remote;

/// <summary>
/// The storage of a run the workers of a remote supervisor hold, reached through its API : what a
/// task asking the user reads and writes from the application it runs in. The queries run where the
/// tables are, only their results travel.
/// <para>
/// The values of the rows cross the API as JSON, which only knows text, numbers and booleans : they
/// are read back as the columns of the table say, a date becoming a date again.
/// </para>
/// </summary>
public sealed class RemoteStorage : IStorage
{
    private readonly SupervisorHttp _http;
    private readonly string _route;

    internal RemoteStorage(SupervisorHttp http, Guid runId)
    {
        _http = http;
        _route = StorageRoutes.Of(runId);
    }

    public async Task<TableRef> CreateTableAsync(string query, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default)
    {
        List<TableRef> tables = await CreateTablesAsync([query], parameters, cancellation);
        return tables[0];
    }

    public Task<List<TableRef>> CreateTablesAsync(IReadOnlyList<string> queries, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default)
    {
        CreateTablesRequest request = new()
        {
            Queries = [.. queries],
            Parameters = parameters == null ? [] : JObject.FromObject(parameters),
        };
        return _http.SendAsync<CreateTablesRequest, List<TableRef>>(HttpMethod.Post, $"{_route}/tables", request);
    }

    public Task<long> CountRowsAsync(TableRef table, CancellationToken cancellation = default)
        => _http.GetAsync<long>($"{_route}/tables/{Escaped(table.Id)}/count");

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ReadRowsAsync(TableRef table,
        [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        await using Stream content = await _http.OpenStreamAsync($"{_route}/tables/{Escaped(table.Id)}/rows");
        using StreamReader reader = new(content);
        while (await reader.ReadLineAsync(cancellation) is string line)
        {
            if (line.Length > 0)
                yield return RowOf(JObject.Parse(line), table.Columns);
        }
    }

    public async Task<TablePage> ReadPageAsync(TableRef table, long offset, int limit, CancellationToken cancellation = default)
    {
        RemoteTablePage page = await _http.GetAsync<RemoteTablePage>($"{_route}/tables/{Escaped(table.Id)}/page?offset={offset}&limit={limit}");
        List<IReadOnlyDictionary<string, object?>> rows = [.. page.Rows.OfType<JObject>().Select(row => RowOf(row, page.Columns))];
        return new TablePage(page.Columns, page.Total, rows);
    }

    public Task<FileRef> SaveFileAsync(string name, Stream content, CancellationToken cancellation = default)
        => _http.PostStreamAsync<FileRef>($"{_route}/files?name={SupervisorHttp.Query(name)}", content);

    public Task<Stream> OpenFileAsync(FileRef file, CancellationToken cancellation = default)
        => _http.OpenStreamAsync($"{_route}/files/{Escaped(file.Id)}/{Escaped(file.Name)}");

    private static string Escaped(string segment) => Uri.EscapeDataString(segment);

    /// <summary>[row] with each value read as its column says, see <see cref="ValueOf"/>.</summary>
    private static Dictionary<string, object?> RowOf(JObject row, IReadOnlyList<TableColumn> columns)
    {
        Dictionary<string, string> types = columns.ToDictionary(x => x.Name, x => x.Type);
        Dictionary<string, object?> values = new(row.Count);
        foreach (JProperty property in row.Properties())
            values[property.Name] = ValueOf(property.Value, types.GetValueOrDefault(property.Name, ""));
        return values;
    }

    /// <summary>
    /// [value] as the storage reads a column of the SQL [type] : the dates and times written as text
    /// read back as such, the rest as JSON reads it.
    /// </summary>
    private static object? ValueOf(JToken value, string type)
    {
        if (value is not JValue { Value: var raw })
            return value.ToString();
        if (raw is not string text)
            return raw;

        if (type == "DATE" && DateOnly.TryParse(text, CultureInfo.InvariantCulture, out DateOnly date))
            return date;
        if (type.StartsWith("TIMESTAMP") && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime moment))
            return moment;
        return text;
    }
}
