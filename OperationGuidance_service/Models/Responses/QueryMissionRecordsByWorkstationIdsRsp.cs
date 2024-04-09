namespace OperationGuidance_service.Models.Responses {
    public class QueryMissionRecordsByWorkstationIdsRsp {
        public Dictionary<int, List<int>> MissionRecordsDict { get; set; }
        public QueryMissionRecordsByWorkstationIdsRsp(Dictionary<int, List<int>> missionRecordsDict) 
            => MissionRecordsDict = missionRecordsDict;
    }
}
