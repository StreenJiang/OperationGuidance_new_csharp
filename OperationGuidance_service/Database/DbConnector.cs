using Microsoft.Data.Sqlite;
using System.Data.SQLite;

namespace OperationGuidance_service.Database {
    public class DbConnector {
        private static readonly string _databaseName = "database.db";
        private static string _defaultDatabasePath = "\\OperationGuidance_service\\Database\\";
        // private static string _defaultDatabasePath = "D:\\VisualStudioProjects\\C#\\OperationGuidance_new\\OperationGuidance_service\\Database\\";
        private static string? _customDatabasePath;
        private static string _ddlSqlFilePath = "\\OperationGuidance_service\\Database\\sqls\\init.sql";
        // private static string _ddlSqlFilePath = "D:\\VisualStudioProjects\\C#\\OperationGuidance_new\\OperationGuidance_service\\Database\\sqls\\init.sql";

        public static string? CustomDatabasePath { get => _customDatabasePath; set => _customDatabasePath = value; }

        private static string GetCurrentDataSource() {
            string dataBasePath;
            if (_customDatabasePath != null) {
                dataBasePath = _customDatabasePath;
            } else {
                string currentProjectDirectory = Directory.GetCurrentDirectory();
                string visualStudioDebugPath = "\\OperationGuidance_new\\bin\\Debug\\net6.0-windows";
                if (currentProjectDirectory.Contains(visualStudioDebugPath)) {
                    currentProjectDirectory = currentProjectDirectory.Replace(visualStudioDebugPath, "");
                }
                dataBasePath = currentProjectDirectory + _defaultDatabasePath;
            }
            dataBasePath += _databaseName;
            return dataBasePath;
        }

        public static SqliteConnection GetConnection() {
            string dataSource = GetCurrentDataSource();
            if (!File.Exists(dataSource)) {
                ExecuteSqlFile(Directory.GetCurrentDirectory() + _ddlSqlFilePath);
            }
            SqliteConnection conn = new SqliteConnection($"Data source = {dataSource}");
            // Console.WriteLine("dataSource: " + dataSource);
            // Console.WriteLine("current directory: " + Directory.GetCurrentDirectory());
            // Console.WriteLine("current project name: " + Assembly.GetCallingAssembly());
            conn.Open();
            return conn;
        }

        public static void ExecuteSqlFile(string sqlFilePath) {
            string commandText = File.ReadAllText(sqlFilePath);
            using (SQLiteConnection conn = new($"Data source = {GetCurrentDataSource()}"))
            using (SQLiteCommand command = conn.CreateCommand()) {
                conn.Open();
                command.CommandText = commandText; 
                command.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}
