using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteOuterDatabaseConfigGlbByIdsReq: ControlRequest {
        public List<int> Ids { get; set; }

        public DeleteOuterDatabaseConfigGlbByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
