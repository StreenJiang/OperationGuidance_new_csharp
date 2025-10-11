using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class BatchAddSqlExecuteRecordsReq: ControlRequest {
        public List<SqlExecuteRecordDTO> SqlExecuteRecordDTOs { get; set; }

        public BatchAddSqlExecuteRecordsReq(List<SqlExecuteRecordDTO> sqlExecuteRecordDTOs) {
            SqlExecuteRecordDTOs = sqlExecuteRecordDTOs;
        }
    }
}
