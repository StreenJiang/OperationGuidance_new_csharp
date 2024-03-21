using MySql.Data.MySqlClient;
using OperationGuidance_service.Database.AbstractClasses;
using System.Data.Common;

namespace OperationGuidance_service.Database {
    public class MySqlConnector: ADbConnector {
        private static readonly string _server = "localhost";
        private static readonly string _port = "3307";
        private static readonly string _database = "aneng";
        private static readonly string _user = "aneng";
        private static readonly string _password = "aneng123";

        public override DbConnection GetDbConnection() {
            MySqlConnection conn = new($"server={_server}; port={_port}; database={_database}; user={_user}; password={_password}; charset=UTF8");
            conn.Open();
            return conn;
        }

    }
}
