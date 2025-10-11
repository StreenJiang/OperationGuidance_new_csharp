using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateMissionRecordReq: ControlRequest {
        public MissionRecordDTO MissionRecordDTO { get; set; }

        public AddOrUpdateMissionRecordReq(MissionRecordDTO dto) {
            MissionRecordDTO = dto;
        }
    }
}
