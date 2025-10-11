using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateScrewBitCounterReq: ControlResponse {
        public ScrewBitCounterDTO ScrewBitCounterDTO { get; set; }

        public AddOrUpdateScrewBitCounterReq(ScrewBitCounterDTO curveDataDTO) {
            ScrewBitCounterDTO = curveDataDTO;
        }
    }
}
