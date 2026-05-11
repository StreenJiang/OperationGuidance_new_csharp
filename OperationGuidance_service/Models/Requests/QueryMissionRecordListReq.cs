using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryMissionRecordListReq: HttpRequest {
        public int? UserId { get; set; }
        public List<int>? Ids { get; set; }
        public DateTime? Date { get; set; }
        public int? MissionId { get; set; }
        public string? ProductBatch { get; set; }
        // 分页
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        // 过滤
        public string? ProductBarCode { get; set; }
        public string? PartsBarCode { get; set; }
        public string? MissionName { get; set; }
        public bool? IsChallengeMission { get; set; }
        public DateTime? CreateTimeMin { get; set; }
        public DateTime? CreateTimeMax { get; set; }
    }
}
