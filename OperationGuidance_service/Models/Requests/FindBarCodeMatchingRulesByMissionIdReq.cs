using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindBarCodeMatchingRulesByMissionIdReq: HttpRequest {
        public int MissionId { get; set; }
    }
}
