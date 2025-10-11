using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryLatestMissionRecordReq: ControlRequest {
        public int UserId { get; set; }

        public QueryLatestMissionRecordReq(int userId) {
            UserId = userId;
        }
    }
}
