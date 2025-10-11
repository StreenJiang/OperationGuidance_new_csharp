using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceToolListReq: ControlRequest {
        public bool ForTask { get; set; } = false;
        public int MacsId { get; set; }
        public QueryDeviceToolListReq(int macsId) => MacsId = macsId;
    }
}
