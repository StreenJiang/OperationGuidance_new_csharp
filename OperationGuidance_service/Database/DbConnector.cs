using log4net;
using OperationGuidance_service.Configurations;
using OperationGuidance_service.Database.AbstractClasses;
using OperationGuidance_service.Exceptions;
using OperationGuidance_service.Utils;
using System.Data.Common;

namespace OperationGuidance_service.Database {
    public static class DbConnector {
        private static ILog logger = SystemUtils.GetLogger(typeof(DbConnector));
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
            DbConnection? dbConnection = connector.GetDbConnection();

            int tryMaxTimes = 3;
            int tryTimes = 0;
            while (tryTimes <= tryMaxTimes) {
                try {
                    dbConnection = connector.GetDbConnection();
                    if (dbConnection != null) {
                        break;
                    }
                    tryTimes++;
                    logger.Warn($"Can not connect to DB, reconnecting... tryTimes = {tryTimes}");
                } catch (DatabaseException de) {
                    logger.Error($"Can not connect to DB, please check DB config or network status. Error message: {de}");
                    continue;
                }
            }

            if (dbConnection == null) {
                throw new DatabaseException("数据库连接失败，请检查配置与数据库信息是否匹配");
            }
            return dbConnection;
        }
    }
}
