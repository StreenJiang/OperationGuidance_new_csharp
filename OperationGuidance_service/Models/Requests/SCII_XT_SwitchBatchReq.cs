using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class SCII_XT_SwitchBatchReq: HttpRequest {
        public string batchNo { get; set; } = string.Empty;
    }
}
