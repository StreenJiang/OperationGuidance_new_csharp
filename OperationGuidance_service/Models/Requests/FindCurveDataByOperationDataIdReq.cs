using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindCurveDataByOperationDataIdReq : HttpRequest {
        public int OperationDataId { get; set; }

        public FindCurveDataByOperationDataIdReq(int operationDataId) {
            OperationDataId = operationDataId;
        }
    }
}
