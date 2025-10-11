using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceArmListReq: ControlRequest {
        public bool ForTask { get; set; } = false;
        public int MacsId { get; set; }
        public QueryDeviceArmListReq(int macsId) => MacsId = macsId;
    }
}
