using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindUserByConditionForCheckingReq: HttpRequest {
        public int Id { get; set; }
        public int? StaffId { get; set; }
        public string? Account { get; set; }
    }
}
