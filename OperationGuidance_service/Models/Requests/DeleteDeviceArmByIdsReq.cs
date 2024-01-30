using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceArmByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceArmByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
