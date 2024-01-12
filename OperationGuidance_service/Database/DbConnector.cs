using System.Data.SQLite;

namespace OperationGuidance_service.Database {
    public class DbConnector {
        private static readonly string _databaseName = "database.db";
        private static string _defaultDatabasePath = "\\OperationGuidance_service\\Database\\";
        private static string? _customDatabasePath;

        public static string? CustomDatabasePath { get => _customDatabasePath; set => _customDatabasePath = value; }

        private static string GetCurrentDataSourcePath() {
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
            return dataBasePath;
        }

        public static SQLiteConnection GetConnection() {
            string dataSourcePath = GetCurrentDataSourcePath();
            string dataSource = dataSourcePath + _databaseName;
            if (!File.Exists(dataSource)) {
                ExecuteSqlFile();
            }
            SQLiteConnection conn = new($"Data source = {dataSource}; UseUTF16Encoding = True;");
            conn.Open();
            return conn;
        }

        public static void ExecuteSqlFile() {
            string dataSourcePath = GetCurrentDataSourcePath();
            if (!Directory.Exists(dataSourcePath)) {
                Directory.CreateDirectory(dataSourcePath);
            }
            string dataSource = dataSourcePath + _databaseName;
            string commandText = Resource.init;
            using (SQLiteConnection conn = new($"Data source = {dataSource}; UseUTF16Encoding = True;"))
            using (SQLiteCommand command = conn.CreateCommand()) {
                try {
                    conn.Open();
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                } catch (Exception e) {
                    throw new Exception($"Data source = {GetCurrentDataSourcePath()}");
                } finally { 
                    conn.Close();
                }
            }
        }
    }
}
