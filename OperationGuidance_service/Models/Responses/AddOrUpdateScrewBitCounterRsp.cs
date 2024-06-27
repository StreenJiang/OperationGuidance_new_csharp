using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateScrewBitCounterRsp: HttpResponse {
        public ScrewBitCounterDTO ScrewBitCounterDTO { get; set; }

        public AddOrUpdateScrewBitCounterRsp(ScrewBitCounterDTO curveDataDTO) {
            ScrewBitCounterDTO = curveDataDTO;
        }
    }
}
