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

        private bool doubleChecked = false;

        public override DbConnection? GetDbConnection() {
            try {
                SqlConnection conn = new(@$"
                        Server={Server},{Port}; 
                        Database={Database}; 
                        User Id={User}; 
                        Password={Password}; 
                        Connect Timeout=2;
                    ");
                conn.Open();

                if (!ConnectionUtils.HealthChecked) {
                    string sqlScriptPrefix = "modify_sqlserver";
                    if (!ConnectionUtils.CheckTableExists(conn, new UserAccountInfo().TableName())) {
                        if (!doubleChecked) {
                            if (SystemUtils.ShowConfirmPopUp("检测到数据库中不存在【用户信息表】，是否执行数据库初始化操作？\n\n（如数据库连接不稳定，可能会导致此检测出现误判。遇到此情况可重启软件。如若持续出现这个情况，请联系管理员）")) {
                                if (SystemUtils.GetDBInitEnabled()) {
                                    string[] batches = Resource.init_sqlserver.Split(new[] { "GO\r\n", "GO\n" }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (string batch in batches) {
                                        if (!string.IsNullOrWhiteSpace(batch)) {
                                            using (SqlCommand command = new SqlCommand(batch, conn)) {
                                                command.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                    SystemUtils.SetDBInitEnabled(false);
                                    SystemUtils.ShowNoticePopUp("数据库初始化完成！已自动禁用数据库初始化功能！");
                                } else {
                                    SystemUtils.ShowNoticePopUp("数据库初始化已经禁用，请联系管理员，检查配置！");
                                    return null;
                                }
                            } else {
                                return null;
                            }
                        }
                        doubleChecked = true;
                    }

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
                    using (SqlCommand command = conn.CreateCommand()) {
                        List<string> fileNames = ConnectionUtils.GetResourcesFileNames();
                        foreach (string fileName in fileNames) {
                            try {
                                if (fileName.Contains(sqlScriptPrefix) && !executedFileNames.Contains(fileName)) {
                                    string? fileText = Resource.ResourceManager.GetString(fileName);
                                    if (!string.IsNullOrEmpty(fileText)) {
                                        logger.Info($"Not executed sql script[{fileName}] found");


                                        string[] batches = fileText.Split(new[] { "GO\r\n", "GO\n" }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (string batch in batches) {
                                            if (!string.IsNullOrWhiteSpace(batch)) {
                                                command.CommandText = batch;
                                                command.ExecuteNonQuery();
                                            }
                                        }

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

                ConnectionUtils.HealthChecked = true;
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
                logger.Error($"Connect to outer sqlserver [server={host}, port={port}] failed, e: {e}");
            }
            return null;
        }
    }
}
