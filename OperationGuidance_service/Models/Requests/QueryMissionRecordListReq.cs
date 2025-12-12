using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    /// <summary>
    /// 查询任务记录列表请求模型
    /// </summary>
    public class QueryMissionRecordListReq: PaginationRequestBase {
        public int? UserId { get; set; }
        public List<int>? Ids { get; set; }
        public DateTime? Date { get; set; }
        public int? MissionId { get; set; }
        public string? ProductBatch { get; set; }

        // 【分页查询】搜索条件
        public string? ProductBarCode { get; set; }      // 总成码/追溯码
        public string? PartsBarCode { get; set; }        // 物料码
        public string? MissionName { get; set; }         // 任务名称
        public bool? IsChallengeMission { get; set; }    // 是否挑战任务
    }
}
