using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class CheckIfBarCodeExistsInMissionRecordReq: HttpRequest {
        public string ProductBarCode { get; set; }
    }
}
