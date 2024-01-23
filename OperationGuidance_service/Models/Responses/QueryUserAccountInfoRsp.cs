using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryUserAccountInfoListRsp: HttpResponse {
        public List<UserAccountInfoDTO> UserAccountInfoDTOs {
            get; set;
        }
    }
}
