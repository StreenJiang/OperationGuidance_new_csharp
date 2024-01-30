using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteDeviceSerialPortByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteDeviceSerialPortByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
