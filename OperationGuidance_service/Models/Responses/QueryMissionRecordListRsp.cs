using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryMissionRecordListRsp: ControlResponse {
        public List<MissionRecordDTO> MissionRecordDTOs {
            get; set;
        }
    }
}
