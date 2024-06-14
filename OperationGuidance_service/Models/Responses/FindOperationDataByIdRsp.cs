using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class FindOperationDataByIdRsp : HttpResponse {
        public OperationDataDTO? OperationDataDTO { get; set; }

        public FindOperationDataByIdRsp(OperationDataDTO? operationDataDTO) {
            OperationDataDTO = operationDataDTO;
        }
    }
}
