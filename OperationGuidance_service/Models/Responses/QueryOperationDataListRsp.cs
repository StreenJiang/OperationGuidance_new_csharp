using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryOperationDataListRsp: HttpResponse {
        public List<OperationDataDTO> OperationDataDTOs { get; set; }
        public int TotalCount { get; set; }

        public QueryOperationDataListRsp(List<OperationDataDTO> operationDataDTOs) => OperationDataDTOs = operationDataDTOs;
    }
}
