using Automation.Services.Local;
using Automation.Services.Local.Database;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Automation.Services.Local.Database
{
    public class DatabaseFactory
    {
        static DatabaseFactory()
        {
            // SQLite has no type of its own for a Guid : it is stored as text, which Dapper has to
            // be told how to read back since the reader hands it over as a plain string.
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

        private readonly string _connectionString;
        public DatabaseFactory(string sqliteDbPath) {
            string? parentFolder = Path.GetDirectoryName(sqliteDbPath) ?? throw new ArgumentException("Invalid sqlite database path.");
            if (Directory.Exists(parentFolder) == false)
                Directory.CreateDirectory(parentFolder);

            _connectionString = $"Data Source={sqliteDbPath}";

            // TODO : ensure schema + seed
        }

        public IDbConnection Create()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public static void WarmUpDatabase(DatabaseFactory factory)
        {
            using var connection = factory.Create();
            // forces native lib load, JIT, pool init, file open
            connection.Execute("SELECT 1;");
        }

        /// <summary>
        /// Read and write a Guid as the text SQLite stores it, whatever it was written with.
        /// </summary>
        private sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value)
            {
                parameter.DbType = DbType.String;
                parameter.Value = value.ToString();
            }

            public override Guid Parse(object value) => value switch
            {
                string text => Guid.Parse(text),
                byte[] bytes => new Guid(bytes),
                _ => (Guid)value,
            };
        }
    }
}

/// <summary>
/// The tables the local database is made of, created when they are missing : the application
/// carries its own schema and there is no migration to run.
/// </summary>
public static class DatabaseSchema
{
    /// <summary>
    /// Create whatever is missing in the database. Called once, before anything reads or writes,
    /// the tables being created in the order they point at each other.
    /// </summary>
    public static void EnsureCreated(DatabaseFactory factory)
    {
        using var connection = factory.Create();
        connection.Execute(TaskInstanceModel.Schema);
    }
}

public static class DatabaseSeeder
{
    // TODO
}