using System.Data.Common;

namespace OperationGuidance_service.Database.AbstractClasses {
    public abstract class ADbConnector {
        public abstract DbConnection? GetDbConnection();

        public abstract DbConnection? GetOuterDbConnection(string host, int port, string databaseName, string? username = null, string? password = null);
    }
}
