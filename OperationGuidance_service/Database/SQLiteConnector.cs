using OperationGuidance_service.Database.AbstractClasses;
using OperationGuidance_service.Utils;
using System.Data.Common;
using System.Data.SQLite;

namespace OperationGuidance_service.Database {
    public class SQLiteConnector: ADbConnector {
        public static string Database = string.Empty;
        public static string Path = string.Empty;

        public override DbConnection? GetDbConnection() {
            string dataSourcePath = GetCurrentDataSourcePath();
            string dataSource = dataSourcePath + Database;
            if (!File.Exists(dataSource)) {
                if (!ExecuteSqlFile()) {
                    return null;
                }
            }
            SQLiteConnection conn = new($"Data source = {dataSource}; UseUTF16Encoding = True; Connection Timeout=2;");
            conn.Open();
            return conn;
        }
        private static string GetCurrentDataSourcePath() => CommonUtils.GetBaseDirectory() + Path;

        public static bool ExecuteSqlFile() {
            string dataSourcePath = GetCurrentDataSourcePath();
            if (!Directory.Exists(dataSourcePath)) {
                Directory.CreateDirectory(dataSourcePath);
            }
            string dataSource = dataSourcePath + Database;
            using (SQLiteConnection conn = new($"Data source = {dataSource}; UseUTF16Encoding = True; Connection Timeout=2;"))
            using (SQLiteCommand command = conn.CreateCommand()) {
                try {
                    conn.Open();
                    command.CommandText = ConnectionUtils.GetInitializationSql("init_sqlite", "modify_sqlite");
                    command.ExecuteNonQuery();
                    return true;
                } catch (Exception e) {
                    System.Console.WriteLine($"e: {e}, Data source = {GetCurrentDataSourcePath()}");
                } finally {
                    conn.Close();
                }
                return false;
            }
        }
    }
}
