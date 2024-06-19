using log4net;
using OperationGuidance_service.Database.AbstractClasses;
using OperationGuidance_service.Utils;
using System.Data.Common;
using System.Data.SQLite;

namespace OperationGuidance_service.Database {
    public class SQLiteConnector: ADbConnector {
        private static ILog logger = LogManager.GetLogger(typeof(SQLiteConnector));

        public static string Database = string.Empty;
        public static string Path = string.Empty;

        public override DbConnection? GetDbConnection() {
            string dataSourcePath = GetCurrentDataSourcePath();
            string dataSource = dataSourcePath + Database;

            SQLiteConnection? conn = null;
            if (!File.Exists(dataSource)) {
                if (!ExecuteSqlFile()) {
                    return null;
                }
            } else {
                conn = new($"Data source = {dataSource}; UseUTF16Encoding = True; Connection Timeout=2;");
                conn.Open();
                string sqlScriptPrefix = "modify_sqlite";

                // Check if any modification scripts hasn't been executed
                List<string> executedFileNames = new();
                using (SQLiteCommand command = conn.CreateCommand()) {
                    command.CommandText = "Select file_name from sql_execute_record where deleted = 2";
                    using (SQLiteDataReader reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            executedFileNames.Add(CommonUtils.CannotBeNull(reader["file_name"].ToString()));
                        }
                    }
                }

                // Execute scripts that didn't execute
                List<string> newExecutedSqlFileName = new();
                using (SQLiteCommand command = conn.CreateCommand()) {
                    List<string> fileNames = ConnectionUtils.GetResourcesFileNames();
                    foreach (string fileName in fileNames) {
                        try {
                            if (fileName.Contains(sqlScriptPrefix) && !executedFileNames.Contains(fileName)) {
                                string? fileText = Resource.ResourceManager.GetString(fileName);
                                if (!string.IsNullOrEmpty(fileText)) {
                                    logger.Info($"Not executed sql script[{fileName}] found");
                                    newExecutedSqlFileName.Add(fileName);
                                    command.CommandText = fileText;
                                    command.ExecuteNonQuery();

                                    logger.Info($"Execute sql script[{fileName}] successfully");
                                    // newExecutedSqlFileName.Add(fileName);
                                }
                            }
                        } catch (Exception e) {
                            logger.Warn($"Execute sql script[{fileName}] failed, e: {e}");
                        }
                    }
                }

                if (newExecutedSqlFileName.Count > 0) {
                    using (SQLiteCommand command = conn.CreateCommand()) {
                        string insertSql = "";
                        foreach (string fileName in newExecutedSqlFileName) {
                            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            insertSql += $"insert into sql_execute_record(file_name, user_id, deleted, creator, modifier, create_time, modify_time) values('{fileName}', -1, 2, 'System', 'System', '{now}', '{now}');";
                        }
                        command.CommandText = insertSql;
                        command.ExecuteNonQuery();
                    }
                }
            }

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
                    command.CommandText = Resource.init_sqlite;
                    command.ExecuteNonQuery();
                    return true;
                } catch (Exception e) {
                    logger.Error($"error for Data source = {GetCurrentDataSourcePath()}: {e}");
                } finally {
                    conn.Close();
                }
                return false;
            }
        }

        public override DbConnection? GetOuterDbConnection(string host, int port, string databaseName, string? username = null, string? password = null) {
            try {
                SQLiteConnection conn = new($"Data source = {host}; UseUTF16Encoding = True; Connection Timeout=2;");
                conn.Open();
                return conn;
            } catch (Exception e) {
                logger.Error($"Connect to outer sqlite for Data source = {host}: {e}");
            }
            return null;
        }
    }
}
