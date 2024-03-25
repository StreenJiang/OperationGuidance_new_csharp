using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceToolListReq: HttpRequest {
        public bool IncludingDeleted { get; set; } = false;
    }
}
