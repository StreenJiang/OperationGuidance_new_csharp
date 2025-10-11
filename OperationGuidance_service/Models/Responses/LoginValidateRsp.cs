using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class LoginValidateRsp: ControlResponse {
        public bool Succeed { get; set; }
        public string FailedReason { get; set; } = string.Empty;
        public UserAccountInfoDTO? UserAccountInfoDTO { get; set; }
    }
}
