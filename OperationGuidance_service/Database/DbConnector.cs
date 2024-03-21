using OperationGuidance_service.Configurations;
using OperationGuidance_service.Database.AbstractClasses;
using System.Data.Common;

namespace OperationGuidance_service.Database {
    public static class DbConnector {
        private static readonly ADbConnector connector;

        static DbConnector() {
            switch (DBConfig.DBType) {
                default:
                case DBTypes.SQLITE:
                    connector = new SQLiteConnector();
                    break;
                case DBTypes.MYSQL:
                    connector = new MySqlConnector();
                    break;
            }
        }

        public static DbConnection GetConnection() => connector.GetDbConnection();
    }
}
