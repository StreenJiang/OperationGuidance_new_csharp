using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public partial class QueryProductMissionDetailReq: ControlRequest {
        public int MissionId { get; set; }
        public QueryProductMissionDetailReq(int missionId) => MissionId = missionId;
    }
}
