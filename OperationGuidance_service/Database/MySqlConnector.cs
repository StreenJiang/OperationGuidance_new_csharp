using log4net;
using MySql.Data.MySqlClient;
using OperationGuidance_service.Database.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Utils;
using System.Data.Common;

namespace OperationGuidance_service.Database {
    public class MySqlConnector: ADbConnector {
        private static ILog logger = LogManager.GetLogger(typeof(MySqlConnector));

        public static string Server = string.Empty;
        public static string Port = string.Empty;
        public static string Database = string.Empty;
        public static string User = string.Empty;
        public static string Password = string.Empty;

        public override DbConnection? GetDbConnection() {
            try {
                MySqlConnection conn = new($"server={Server}; port={Port}; database={Database}; user={User}; password={Password}; charset=UTF8; Connection Timeout=2;");
                conn.Open();

                string sqlScriptPrefix = "modify_mysql";
                if (!ConnectionUtils.CheckTableExists(conn, new UserAccountInfo().TableName())) {
                    using (MySqlCommand command = conn.CreateCommand()) {
                        command.CommandText = Resource.init_mysql;
                        command.ExecuteNonQuery();
                    }
                } else {
                    // Check if any modification scripts hasn't been executed
                    List<string> executedFileNames = new();
                    using (MySqlCommand command = conn.CreateCommand()) {
                        command.CommandText = "Select file_name from sql_execute_record where deleted = 2";
                        using (MySqlDataReader reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                executedFileNames.Add(CommonUtils.CannotBeNull(reader["file_name"].ToString()));
                            }
                        }
                    }

                    // Execute scripts that didn't execute
                    List<string> newExecutedSqlFileName = new();
                    using (MySqlCommand command = conn.CreateCommand()) {
                        List<string> fileNames = ConnectionUtils.GetResourcesFileNames();
                        foreach (string fileName in fileNames) {
                            try {
                                if (fileName.Contains(sqlScriptPrefix) && !executedFileNames.Contains(fileName)) {
                                    string? fileText = Resource.ResourceManager.GetString(fileName);
                                    if (!string.IsNullOrEmpty(fileText)) {
                                        logger.Info($"Not executed sql script[{fileName}] found");
                                        command.CommandText = fileText;
                                        command.ExecuteNonQuery();

                                        logger.Info($"Execute sql script[{fileName}] successfully");
                                        newExecutedSqlFileName.Add(fileName);
                                    }
                                }
                            } catch (Exception e) {
                                logger.Warn($"Execute sql script[{fileName}] failed, e: {e}");
                            }
                        }
                    }

                    if (newExecutedSqlFileName.Count > 0) {
                        using (MySqlCommand command = conn.CreateCommand()) {
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
            } catch (Exception e) {
                logger.Error($"Connect to mysql [server={Server}, port={Port}] failed, e: {e}");
            }
            return null;
        }
    }
}
