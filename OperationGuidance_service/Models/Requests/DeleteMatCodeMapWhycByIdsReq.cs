using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteMatCodeMapWhycByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteMatCodeMapWhycByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
