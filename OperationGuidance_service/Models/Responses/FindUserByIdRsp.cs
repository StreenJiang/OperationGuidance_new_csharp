using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class FindUserByIdRsp: HttpResponse {
        public UserAccountInfoDTO UserAccountInfoDTO {
            get; set;
        }

        public FindUserByIdRsp() {
            UserAccountInfoDTO = new();
        }
    }
}
