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
}
