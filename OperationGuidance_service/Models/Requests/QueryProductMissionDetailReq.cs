using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public partial class QueryProductMissionDetailReq: ControlRequest {
        public int MissionId { get; set; } = -1;
        public string? MissionName { get; set; }
        public QueryProductMissionDetailReq(int missionId) => MissionId = missionId;
        public QueryProductMissionDetailReq(string missionName) => MissionName = missionName;
    }
}
