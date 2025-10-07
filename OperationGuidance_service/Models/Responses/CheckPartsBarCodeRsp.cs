using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class CheckPartsBarCodeRsp: HttpResponse {
        public bool Yes { get; set; }

        public CheckPartsBarCodeRsp(bool yes) => Yes = yes;
    }
}
