using Fantoche.Services.Local.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Fantoche.Services.Local.Database
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

            // Kept by the file once set : the writes stop waiting on the readers and on a sync of the
            // whole file each, which is what bounds the history a supervisor stores.
            using (var connection = Create())
                connection.Execute("PRAGMA journal_mode=WAL;");

            // The database is ready to be read from as soon as the factory is built : the tables
            // are created when they are missing, and a database that never held anything is given
            // its starting content.
            DatabaseSchema.EnsureCreated(this);
            DatabaseSeeder.Seed(this);
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
            DropOutdatedTables(connection);

            // The tree comes first : the instances point at the element they ran, and the nodes of a
            // graph at the workflow holding them.
            connection.Execute(ScopedModel.Schema);
            connection.Execute(TaskInstanceModel.Schema);
            connection.Execute(GraphNodeModel.Schema);
            connection.Execute(GraphConnectorModel.Schema);
            connection.Execute(GraphConnectionModel.Schema);
            connection.Execute(GraphVersionModel.Schema);

            RenameLegacyColumns(connection);
        }

        /// <summary>
        /// Drop the tables an older version wrote without a column the current one needs, so they
        /// are created again : the application is still in alpha, losing their rows is accepted.
        /// </summary>
        private static void DropOutdatedTables(IDbConnection connection)
        {
            // What a resumed run reads of its history.
            DropWithout(connection, "TaskInstances", "SessionId");
        }

        private static void DropWithout(IDbConnection connection, string table, string column)
        {
            List<string> columns = [.. connection.Query<string>($"SELECT name FROM pragma_table_info('{table}');")];
            if (columns.Count == 0 || columns.Contains(column))
                return;

            connection.Execute($"DROP TABLE {table};");
        }

        /// <summary>
        /// Rename what a database written by an older version calls otherwise : creating the tables
        /// leaves an existing one untouched, so a renamed column would never reach it and every
        /// query naming it would fail. The only thing this handles is a rename, a column that is
        /// merely gone being harmless (it stays there, nullable and unread).
        /// </summary>
        private static void RenameLegacyColumns(IDbConnection connection)
        {
            Rename(connection, "GraphNodes", "ParametersJson", "InputMappingJson");
        }

        /// <summary>
        /// Rename [column] of [table] into [renamed], when the table still holds the old name and
        /// not the new one : anything else means there is nothing to do.
        /// </summary>
        private static void Rename(IDbConnection connection, string table, string column, string renamed)
        {
            List<string> columns = [.. connection.Query<string>($"SELECT name FROM pragma_table_info('{table}');")];
            if (!columns.Contains(column) || columns.Contains(renamed))
                return;

            connection.Execute($"ALTER TABLE {table} RENAME COLUMN {column} TO {renamed};");
        }
    }
}
