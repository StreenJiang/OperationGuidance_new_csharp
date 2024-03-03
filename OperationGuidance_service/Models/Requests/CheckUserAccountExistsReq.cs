using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class CheckUserAccountExistsReq: HttpRequest {
        public string? Account { get; set; }
    }
}
