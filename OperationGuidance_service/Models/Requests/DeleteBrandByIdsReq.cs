using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteBrandByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteBrandByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
