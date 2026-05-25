using OperationGuidance_service.Models.Responses;

namespace OperationGuidance_service.Models.Requests {
    public class ReimportPartsBarcodeReq {
        public Action<ReimportProgressInfo>? OnProgress { get; set; }
    }
}
