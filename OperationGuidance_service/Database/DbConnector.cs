using OperationGuidance_service.Configurations;
using OperationGuidance_service.Database.AbstractClasses;
using OperationGuidance_service.Exceptions;
using OperationGuidance_service.Utils;
using System.Data.Common;

namespace OperationGuidance_service.Database {
    public static class DbConnector {
        private readonly static int _retryTimes = 2;
        private readonly static int _retryDelay = 500;
        private static readonly ADbConnector connector;

        static DbConnector() {
            SystemUtils.InitMySqlConfigs();
            SystemUtils.InitSQLiteConfigs();
            switch (SystemUtils.GetDBTypes()) {
                default:
                case DBTypes.SQLITE:
                    connector = new SQLiteConnector();
                    break;
                case DBTypes.MYSQL:
                    connector = new MySqlConnector();
                    break;
            }
        }

        public static DbConnection GetConnection() {
            int tryCount = 0;
            DbConnection? dbConnection = null;

            while (tryCount <= _retryTimes) {
                dbConnection = connector.GetDbConnection();
                if (dbConnection != null) {
                    break;
                }
                tryCount++;
                Thread.Sleep(_retryDelay);
            }

            if (dbConnection == null) {
                throw new DatabaseException("数据库连接失败，请检查配置与数据库信息是否匹配");
            }
            return dbConnection;
        }

        public static bool CheckConnection() => GetConnection() == null;
    }
}
