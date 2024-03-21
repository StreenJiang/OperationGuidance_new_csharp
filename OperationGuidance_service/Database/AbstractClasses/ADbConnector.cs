using System.Data.Common;

namespace OperationGuidance_service.Database.AbstractClasses {
    public abstract class ADbConnector {
        public abstract DbConnection GetDbConnection();
    }
}
