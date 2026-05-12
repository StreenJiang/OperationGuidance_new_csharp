using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryOperationDataListReq: ControlRequest {
        public int? UserId { get; set; }
        public int? MissionRecordId { get; set; }
        // 分页
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        // 过滤
        public string? VinNumber { get; set; }
        public DateTime? CreateTimeMin { get; set; }
        public DateTime? CreateTimeMax { get; set; }
        public int? WorkstationId { get; set; }
    }
}
