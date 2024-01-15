using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceModelByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceModelByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
