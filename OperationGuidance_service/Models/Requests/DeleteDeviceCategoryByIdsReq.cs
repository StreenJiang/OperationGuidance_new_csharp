using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceCategoryByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceCategoryByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
