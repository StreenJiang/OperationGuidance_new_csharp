using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class FindScrewBitCounterByMissionIdRsp: HttpResponse {
        public List<ScrewBitCounterDTO> ScrewBitCounterDTOs { get; set; } = new();
    }
}
