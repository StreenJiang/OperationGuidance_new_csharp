using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceCommunicationByIdsReq: ControlRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceCommunicationByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
