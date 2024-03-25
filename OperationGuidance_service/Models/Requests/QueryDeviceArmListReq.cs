using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceArmListReq: HttpRequest {
        public bool IncludingDeleted { get; set; } = false;
    }
}
