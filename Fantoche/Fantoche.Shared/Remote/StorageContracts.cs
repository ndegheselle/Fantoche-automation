using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Remote;

/// <summary>
/// The storage of a run as it crosses the API of the supervisor (see <see cref="StorageRoutes"/>) :
/// what an application reads and writes of a run its workers hold, for a task asking the user.
/// </summary>
public static class StorageRoutes
{
    public static string Of(Guid runId) => $"storage/runs/{runId}";
}

/// <summary>The tables to create, see <see cref="IStorage.CreateTablesAsync"/>.</summary>
public sealed class CreateTablesRequest
{
    public List<string> Queries { get; set; } = [];

    /// <summary>The values of the parameters, as JSON : their types are the ones JSON knows.</summary>
    public JObject Parameters { get; set; } = [];
}

/// <summary>A <see cref="TablePage"/>, its rows written as JSON.</summary>
public sealed class RemoteTablePage
{
    public List<TableColumn> Columns { get; set; } = [];
    public long Total { get; set; }
    public JArray Rows { get; set; } = [];
}
