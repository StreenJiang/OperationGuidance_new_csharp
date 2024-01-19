using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class QueryOperationDataListRsp: HttpResponse {
        public List<OperationDataDTO> OperationDataDTOs {
            get; set;
        }
    }
}
