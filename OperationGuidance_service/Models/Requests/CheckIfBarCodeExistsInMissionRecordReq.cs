using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class CheckIfBarCodeExistsInMissionRecordReq: HttpRequest {
        public string? ProductBarCode { get; set; }
        public string? PartsBarCode { get; set; }
        public int MissionId { get; set; }
        public int MissionResult { get; set; }

        public CheckIfBarCodeExistsInMissionRecordReq(int missionId, int missionResult) {
            MissionId = missionId;
            MissionResult = missionResult;
        }
    }
}
