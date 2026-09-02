using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Automation.Services.Local
{
    public class DatabaseFactory
    {
        private readonly string _connectionString;
        public DatabaseFactory(string sqliteDbPath) {
            string? parentFolder = Path.GetDirectoryName(sqliteDbPath) ?? throw new ArgumentException("Invalid sqlite database path.");
            if (Directory.Exists(parentFolder) == false)
                Directory.CreateDirectory(parentFolder);

            _connectionString = $"Data Source={sqliteDbPath}";
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
    }
}
