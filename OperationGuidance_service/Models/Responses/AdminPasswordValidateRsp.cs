using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class AdminPasswordValidateRsp: HttpResponse {
        public bool Succeed { get; set; }
    }
}
