using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class AdminPasswordValidateReq: HttpRequest {
        public string AdminPassword { get; set; }

        public AdminPasswordValidateReq(string adminPassword) {
            AdminPassword = adminPassword;
        }
    }
}
