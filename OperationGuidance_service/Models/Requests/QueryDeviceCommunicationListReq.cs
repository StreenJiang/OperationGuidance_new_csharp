using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceCommunicationListReq: HttpRequest {
        public bool IncludingDeleted { get; set; } = false;
    }
}
