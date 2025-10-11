using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceIoListReq: ControlRequest {
        public bool ForTask { get; set; } = false;
        public int MacsId { get; set; }
        public QueryDeviceIoListReq(int macsId) => MacsId = macsId;
    }
}
