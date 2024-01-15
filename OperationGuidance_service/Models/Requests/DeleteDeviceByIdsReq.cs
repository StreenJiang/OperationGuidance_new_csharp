using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
