using log4net;
using OperationGuidance_service.Database.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Utils;
using System.Data.Common;
using System.Data.SqlClient;

namespace OperationGuidance_service.Database {
    public class SqlServerConnector: ADbConnector {
        private static ILog logger = LogManager.GetLogger(typeof(SqlServerConnector));

        public static string Server = string.Empty;
        public static string Port = string.Empty;
        public static string Database = string.Empty;
        public static string User = string.Empty;
        public static string Password = string.Empty;

        public override DbConnection? GetDbConnection() {
            try {
                SqlConnection conn = new($"Server={Server},{Port}; Database={Database}; User Id={User}; Password={Password}; Connect Timeout=2;");
                conn.Open();

                string sqlScriptPrefix = "modify_sqlserver";
                if (!ConnectionUtils.CheckTableExists(conn, new UserAccountInfo().TableName())) {
                    string[] batches = Resource.init_sqlserver.Split(new[] { "GO\r\n", "GO\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string batch in batches) {
                        if (!string.IsNullOrWhiteSpace(batch)) {
                            using (SqlCommand command = new SqlCommand(batch, conn)) {
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                } else {
                    // Check if any modification scripts hasn't been executed
                    List<string> executedFileNames = new();
                    using (SqlCommand command = conn.CreateCommand()) {
                        command.CommandText = "Select file_name from sql_execute_record where deleted = 2";
                        using (SqlDataReader reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                executedFileNames.Add(CommonUtils.CannotBeNull(reader["file_name"].ToString()));
                            }
                        }
                    }

                    // Execute scripts that didn't execute
                    List<string> newExecutedSqlFileName = new();
                    List<string> fileNames = ConnectionUtils.GetResourcesFileNames();
                    foreach (string fileName in fileNames) {
                        try {
                            if (fileName.Contains(sqlScriptPrefix) && !executedFileNames.Contains(fileName)) {
                                logger.Info($"Not executed sql script[{fileName}] found");
                                string[] batches = File.ReadAllText(sqlScriptPrefix).Split(new[] { "GO\r\n", "GO\n" }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string batch in batches) {
                                    if (!string.IsNullOrWhiteSpace(batch)) {
                                        using (SqlCommand command = new SqlCommand(batch, conn)) {
                                            command.ExecuteNonQuery();
                                        }
                                    }
                                }
                                logger.Info($"Execute sql script[{fileName}] successfully");
                                newExecutedSqlFileName.Add(fileName);

                            }
                        } catch (Exception e) {
                            logger.Warn($"Execute sql script[{fileName}] failed, e: {e}");
                        }
                    }
                    using (SqlCommand command = conn.CreateCommand()) {
                    }

                    if (newExecutedSqlFileName.Count > 0) {
                        using (SqlCommand command = conn.CreateCommand()) {
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
                logger.Error($"Connect to sqlserver [server={Server}, port={Port}] failed, e: {e}");
            }
            return null;
        }

        public override DbConnection? GetOuterDbConnection(string host, int port, string databaseName, string? username = null, string? password = null) {
            try {
                SqlConnection conn = new($"Server={host},{port}; Database={databaseName}; User Id={username}; Password={password}; Connect Timeout=2;");
                conn.Open();
                return conn;
            } catch (Exception e) {
                logger.Error($"Connect to outer sqlserver [server={Server}, port={Port}] failed, e: {e}");
            }
            return null;
        }
    }
}
