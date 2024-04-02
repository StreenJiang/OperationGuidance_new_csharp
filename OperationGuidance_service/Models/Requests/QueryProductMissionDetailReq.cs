using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public partial class QueryProductMissionDetailReq: HttpRequest {
        public int MissionId { get; set; }
        public QueryProductMissionDetailReq(int missionId) => MissionId = missionId; 
    }
}
