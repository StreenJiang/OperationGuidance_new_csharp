using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceToolByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceToolByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
