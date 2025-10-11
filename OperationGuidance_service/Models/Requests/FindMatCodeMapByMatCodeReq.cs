using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindMatCodeMapByMatCodeReq: ControlRequest {
        public string MatCode { get; set; }

        public FindMatCodeMapByMatCodeReq(String matCode) {
            MatCode = matCode;
        }
    }
}
