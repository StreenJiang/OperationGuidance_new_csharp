using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteOuterDatabaseConfigGlbByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteOuterDatabaseConfigGlbByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
