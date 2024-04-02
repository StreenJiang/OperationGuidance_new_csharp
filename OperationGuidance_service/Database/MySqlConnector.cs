using MySql.Data.MySqlClient;
using OperationGuidance_service.Database.AbstractClasses;
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
                MySqlConnection conn = new($"server={Server}; port={Port}; database={Database}; user={User}; password={Password}; charset=UTF8");
                conn.Open();
                return conn;
            } catch (Exception e) {
                System.Console.WriteLine($"Connect to mysql failed, e: {e}");
            }
            return null;
        }

    }
}
