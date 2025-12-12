using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    /// <summary>
    /// 查询操作数据列表请求模型
    /// </summary>
    public class QueryOperationDataListReq: PaginationRequestBase {
        public int? UserId { get; set; }
        public int? MissionRecordId { get; set; }

        // 【分页查询】搜索条件
        public string? VinNumber { get; set; }           // VIN号搜索
        public int? WorkstationId { get; set; }          // 站点ID过滤
        public DateTime? StartDate { get; set; }         // 开始日期
        public DateTime? EndDate { get; set; }           // 结束日期
    }
}
