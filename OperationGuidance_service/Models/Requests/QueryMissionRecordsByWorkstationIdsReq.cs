using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryMissionRecordsByWorkstationIdsReq: HttpRequest {
        public List<int> WorkstationIds {  get; set; }
        public QueryMissionRecordsByWorkstationIdsReq(List<int> workstationIds) => WorkstationIds = workstationIds;
    }
}
