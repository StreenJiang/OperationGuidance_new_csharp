using MySql.Data.MySqlClient;
using OperationGuidance_service.Database.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Utils;
using System.Data.Common;

namespace OperationGuidance_service.Database {
    public class MySqlConnector: ADbConnector {
        public static string Server = string.Empty;
        public static string Port = string.Empty;
        public static string Database = string.Empty;
        public static string User = string.Empty;
        public static string Password = string.Empty;

        public override DbConnection? GetDbConnection() {
            try {
                MySqlConnection conn = new($"server={Server}; port={Port}; database={Database}; user={User}; password={Password}; charset=UTF8; Connection Timeout=2;");
                conn.Open();

                if (!ConnectionUtils.CheckTableExists(conn, new UserAccountInfo().TableName())) {
                    using (MySqlCommand command = conn.CreateCommand()) {
                        command.CommandText = ConnectionUtils.GetInitializationSql("init_mysql", "modify_mysql");
                        command.ExecuteNonQuery();
                    }
                }

                return conn;
            } catch (Exception e) {
                System.Console.WriteLine($"Connect to mysql failed, e: {e}");
            }
            return null;
        }
    }
}
