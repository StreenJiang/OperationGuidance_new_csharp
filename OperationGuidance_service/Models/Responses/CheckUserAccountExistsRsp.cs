using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class CheckUserAccountExistsRsp: HttpResponse {
        public bool Exists {
            get; set;
        } = false;
    }
}
