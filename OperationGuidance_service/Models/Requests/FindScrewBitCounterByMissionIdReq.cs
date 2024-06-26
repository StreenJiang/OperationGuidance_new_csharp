using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindScrewBitCounterByMissionIdReq: HttpRequest {
        public int MissionId { get; set; }

        public FindScrewBitCounterByMissionIdReq(int missionId) {
            MissionId = missionId;
        }
    }
}
