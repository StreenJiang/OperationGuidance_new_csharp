using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateMissionRecordRsp: ControlResponse {
        public MissionRecordDTO MissionRecordDTO { get; set; } = new();
    }
}
