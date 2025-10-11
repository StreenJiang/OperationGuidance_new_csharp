using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteScrewBitCounterReq: ControlRequest {
        public ScrewBitCounterDTO ScrewBitCounterDTO { get; set; }

        public DeleteScrewBitCounterReq(ScrewBitCounterDTO productMissionDTO) {
            ScrewBitCounterDTO = productMissionDTO;
        }
    }
}
