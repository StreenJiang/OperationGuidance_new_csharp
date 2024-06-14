using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;
using OperationGuidance_service.Constants;

namespace OperationGuidance_service.Services {
    [Service]
    public class CurveDataService : AServiceBase<CurveData, CurveDataWrapper> {

        // 根据 operation_id 查询数据
        public List<CurveData> QueryDataByOperationDataId(int operation_data_id) {
            string sql = $"select * from {TableName} where deleted = @deleted and operation_data_id = @operation_data_id";
            Dictionary<string, object> parameters = new() {
                { "deleted", (int)YesOrNo.NO },
                { "operation_data_id", operation_data_id },
            };
            return FindBySql(sql, parameters);
        }

    }
}
