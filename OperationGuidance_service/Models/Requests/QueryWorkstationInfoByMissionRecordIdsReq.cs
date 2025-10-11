using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryWorkstationInfoByMissionRecordIdsReq: ControlRequest {
        public List<int> MissionRecordIds { get; set; }

        public QueryWorkstationInfoByMissionRecordIdsReq(List<int> missionRecordIds) => MissionRecordIds = missionRecordIds;
    }
}
