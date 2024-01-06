using Dapper;
using Microsoft.Data.Sqlite;
using OperationGuidance_service.Models;
using System.Reflection;

namespace OperationGuidance_service.Database {
    public class DbConnector {
        // private static readonly string _databaseName = "database.db";
        // private static string _defaultDatabasePath = "/";
        private static readonly string _databaseName = "test_db.db";
        private static string _defaultDatabasePath = "D:\\VisualStudioProjects\\C#\\OperationGuidance_new\\OperationGuidance_new\\Database\\";
        private static string? _customDatabasePath;

        public static string? CustomDatabasePath { get => _customDatabasePath; set => _customDatabasePath = value; }

        public static SqliteConnection GetConnection() {
            string dataSource = (_customDatabasePath == null ? _defaultDatabasePath : _customDatabasePath) + _databaseName;
            SqliteConnection conn = new SqliteConnection($"Data source={dataSource}");
            // Console.WriteLine("dataSource: " + dataSource);
            // Console.WriteLine("current directory: " + Directory.GetCurrentDirectory());
            // Console.WriteLine("current project name: " + Assembly.GetCallingAssembly());
            conn.Open();
            return conn;
        }

        public static List<ProductMission> test() {
            using (SqliteConnection conn = GetConnection()) {
                System.Console.WriteLine(conn);
                // SqliteTransaction sqliteTransaction = conn.BeginTransaction();

                // string sql = "select * from product_mission where is_deleted != 1";
                // IEnumerable<ProductMission> enumerable = conn.Query<ProductMission>(sql);
                //
                // // sqliteTransaction.Commit();
                // return enumerable.ToList();
            }
            return null;
        }
    }
}
