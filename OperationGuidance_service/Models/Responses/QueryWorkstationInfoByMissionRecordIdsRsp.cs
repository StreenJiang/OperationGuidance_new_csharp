using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class QueryWorkstationInfoByMissionRecordIdsRsp: ControlResponse {
        public Dictionary<int, Dictionary<int, string>> WorkstationInfos { get; set; }

        public QueryWorkstationInfoByMissionRecordIdsRsp(Dictionary<int, Dictionary<int, string>> workstationInfos)
            => WorkstationInfos = workstationInfos;
    }
}
