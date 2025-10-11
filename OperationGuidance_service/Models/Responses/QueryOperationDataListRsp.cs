using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryOperationDataListRsp: ControlResponse {
        public List<OperationDataDTO> OperationDataDTOs { get; set; }

        public QueryOperationDataListRsp(List<OperationDataDTO> operationDataDTOs) => OperationDataDTOs = operationDataDTOs;
    }
}
