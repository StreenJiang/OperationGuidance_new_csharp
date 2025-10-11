using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceSerialPortListReq: ControlRequest {
        public bool ForTask { get; set; } = false;
        public int MacsId { get; set; }
        public QueryDeviceSerialPortListReq(int macsId) => MacsId = macsId;
    }
}
