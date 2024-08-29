using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindMatCodeMapByMatCodeReq: HttpRequest {
        public string MatCode { get; set; }

        public FindMatCodeMapByMatCodeReq(String matCode) {
            MatCode = matCode;
        }
    }
}
