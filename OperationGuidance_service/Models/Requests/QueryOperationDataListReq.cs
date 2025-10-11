using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryOperationDataListReq: ControlRequest {
        public int? UserId { get; set; }
        public int? MissionRecordId { get; set; }
    }
}
