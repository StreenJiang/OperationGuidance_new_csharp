using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryMissionRecordListReq: HttpRequest {
        public int? UserId { get; set; }
        public List<int>? Ids { get; set; }
        public DateTime? Date { get; set; }
        public int? MissionId { get; set; }
    }
}
