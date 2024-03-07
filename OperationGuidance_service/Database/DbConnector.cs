using System.Data.SQLite;

namespace OperationGuidance_service.Database {
    public class DbConnector {
        private static readonly string DatabaseName = "database.db";
        private static readonly string _selfFolder = "Database\\";
        private static string _defaultDatabasePath = "OperationGuidance_service\\" + _selfFolder;

        private static string GetCurrentDataSourcePath() {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string visualStudioDebugPath = "\\OperationGuidance_new\\bin\\Debug\\net6.0-windows";
            if (baseDirectory.Contains(visualStudioDebugPath)) {
                baseDirectory = baseDirectory.Replace(visualStudioDebugPath, "");
            }
            return baseDirectory + _defaultDatabasePath;
        }

        public static SQLiteConnection GetConnection() {
            string dataSourcePath = GetCurrentDataSourcePath();
            string dataSource = dataSourcePath + DatabaseName;
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
            string dataSource = dataSourcePath + DatabaseName;
            string commandText = Resource.init_sqlite;
            using (SQLiteConnection conn = new($"Data source = {dataSource}; UseUTF16Encoding = True;"))
            using (SQLiteCommand command = conn.CreateCommand()) {
                try {
                    conn.Open();
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                } catch (Exception e) {
                    throw new Exception($"e: {e}, Data source = {GetCurrentDataSourcePath()}");
                } finally { 
                    conn.Close();
                }
            }
        }
    }
}
