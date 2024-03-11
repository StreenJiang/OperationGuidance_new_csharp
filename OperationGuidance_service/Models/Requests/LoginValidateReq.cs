using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class LoginValidateReq: HttpRequest {
        public string Account { get; set; }
        public string Password { get; set; }

        public LoginValidateReq(string account, string password) {
            Account = account;
            Password = password;
        }
    }
}
