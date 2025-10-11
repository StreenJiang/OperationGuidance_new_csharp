using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class SwitchBatchReq_SCII_XT: HttpRequest {
        public string batchNo { get; set; } = string.Empty;
    }
}
