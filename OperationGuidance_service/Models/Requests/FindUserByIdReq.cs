using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindUserByIdReq: HttpRequest {
        public int UserId { get; set; }
        public FindUserByIdReq(int userId) {
            UserId = userId;
        }
    }
}
