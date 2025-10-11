using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Models.Requests {
    public class FindUserByIdReq: ControlRequest {
        public int UserId { get; set; } = SystemUtils.LoggedUserId;
    }
}
