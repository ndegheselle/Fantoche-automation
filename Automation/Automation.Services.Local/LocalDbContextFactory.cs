namespace Automation.Services.Local;

/// <summary>
/// Creates short-lived <see cref="LocalDbContext"/> instances against a single SQLite file,
/// seeding the database with its minimal starting elements the first time it is created.
/// </summary>
public class LocalDbContextFactory
{
    private readonly string _connectionString;

    public LocalDbContextFactory(string databaseFilePath)
    {
        // SQLite fails to open the file (SQLITE_CANTOPEN) if its parent directory doesn't exist yet.
        var directory = Path.GetDirectoryName(databaseFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connectionString = $"Data Source={databaseFilePath}";

        using var db = CreateDbContext();
        if (db.Database.EnsureCreated())
            LocalSeed.Seed(db);
    }

    public LocalDbContext CreateDbContext() => new LocalDbContext(_connectionString);

    /// <summary>
    /// Run [query] against a short-lived context, on a thread pool thread. Entity Framework does a
    /// lot of synchronous work before the first real await of an async query (model building, query
    /// compilation, opening the connection), so awaiting one directly from the UI thread still
    /// freezes it : going through here is what keeps the queries actually off that thread.
    /// </summary>
    public Task<T> QueryAsync<T>(Func<LocalDbContext, Task<T>> query) => Task.Run(async () =>
    {
        using var db = CreateDbContext();
        return await query(db);
    });

    /// <summary>
    /// Pay the one-time cost of Entity Framework's setup (model building and the compilation of the
    /// query shapes the pages use) in the background, so the first page needing data doesn't.
    /// </summary>
    public Task WarmupAsync() => Task.Run(() =>
    {
        using var db = CreateDbContext();
        _ = db.ScopedElements.FirstOrDefault();
        _ = db.TaskInstances.FirstOrDefault();
    });
}
