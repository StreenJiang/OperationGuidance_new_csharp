using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    /// <summary>
    /// 查询操作数据列表响应模型
    /// </summary>
    public class QueryOperationDataListRsp: PaginationResponseBase<OperationDataDTO> {
        // 【向后兼容】提供便捷属性，保持现有API兼容
        public List<OperationDataDTO> OperationDataDTOs => Items;
    }
}
