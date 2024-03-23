using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryLatestMissionRecordReq: HttpRequest {
        public int UserId { get; set; }

        public QueryLatestMissionRecordReq(int userId) {
            UserId = userId;
        }
    }
}
