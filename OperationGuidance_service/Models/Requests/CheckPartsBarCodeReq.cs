using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class CheckPartsBarCodeReq: HttpRequest {
        public int MissionId { get; set; }
        public string PartsBarCode { get; set; }

        public CheckPartsBarCodeReq(int missionId, string partsBarCode) {
            MissionId = missionId;
            PartsBarCode = partsBarCode;
        }
    }
}
