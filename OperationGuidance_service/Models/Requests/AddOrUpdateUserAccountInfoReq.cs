using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateUserAccountInfoReq: ControlRequest {
        public UserAccountInfoDTO UserAccountInfoDTO { get; set; }

        public AddOrUpdateUserAccountInfoReq(UserAccountInfoDTO dto) {
            UserAccountInfoDTO = dto;
        }
    }
}
