using OperationGuidance_service.Database.AbstractClasses;
using System.Data.Common;
using System.Data.SQLite;

namespace OperationGuidance_service.Database {
    public class SQLiteConnector: ADbConnector {
        private static readonly string _database = "database.db";
        private static readonly string _defaultDatabasePath = "OperationGuidance_service\\Database\\";

        public override DbConnection GetDbConnection() {
            string dataSourcePath = GetCurrentDataSourcePath();
            string dataSource = dataSourcePath + _database;
            if (!File.Exists(dataSource)) {
                ExecuteSqlFile();
            }
            SQLiteConnection conn = new($"Data source = {dataSource}; UseUTF16Encoding = True;");
            conn.Open();
            return conn;
        }

        private static string GetCurrentDataSourcePath() {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string visualStudioDebugPath = "\\OperationGuidance_new\\bin\\Debug\\net6.0-windows";
            if (baseDirectory.Contains(visualStudioDebugPath)) {
                baseDirectory = baseDirectory.Replace(visualStudioDebugPath, "");
            }
            string visualStudioDebugPath2 = "\\bin\\Debug\\net6.0-windows";
            if (baseDirectory.Contains(visualStudioDebugPath2)) {
                baseDirectory = baseDirectory.Replace(visualStudioDebugPath2, "");
            }

            return baseDirectory + _defaultDatabasePath;
        }

        public static void ExecuteSqlFile() {
            string dataSourcePath = GetCurrentDataSourcePath();
            if (!Directory.Exists(dataSourcePath)) {
                Directory.CreateDirectory(dataSourcePath);
            }
            string dataSource = dataSourcePath + _database;
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
