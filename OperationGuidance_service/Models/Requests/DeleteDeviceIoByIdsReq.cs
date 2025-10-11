using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceIoByIdsReq: ControlRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceIoByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
