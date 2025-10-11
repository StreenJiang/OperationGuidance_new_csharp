using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddDataToOuterDatabaseGlbReq: ControlRequest {
        public OuterDatabaseConfigGlbDTO OuterDatabaseConfigGlbDTO { get; set; }
        public MissionRecordDTO MissionRecordDTO { get; set; }
        public List<OperationDataDTO> OperationDataDTOs { get; set; }

        public AddDataToOuterDatabaseGlbReq(OuterDatabaseConfigGlbDTO outerDatabaseConfigGlbDTO, MissionRecordDTO missionRecordDTO, List<OperationDataDTO> operationDataDTOs) {
            OuterDatabaseConfigGlbDTO = outerDatabaseConfigGlbDTO;
            MissionRecordDTO = missionRecordDTO;
            OperationDataDTOs = operationDataDTOs;
        }
    }
}
