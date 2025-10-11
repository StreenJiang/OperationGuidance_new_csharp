using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QuerySqlExecuteRecordListRsp: ControlResponse {
        public List<SqlExecuteRecordDTO> SqlExecuteRecordsDTOs { get; set; }

        public QuerySqlExecuteRecordListRsp(List<SqlExecuteRecordDTO> sqlExecuteRecordsDTOs) => SqlExecuteRecordsDTOs = sqlExecuteRecordsDTOs;
    }
}
