using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateBarCodeMatchingRuleReq: ControlRequest {
        public BarCodeMatchingRuleDTO BarCodeMatchingRuleDTO { get; set; }

        public AddOrUpdateBarCodeMatchingRuleReq(BarCodeMatchingRuleDTO dto) {
            BarCodeMatchingRuleDTO = dto;
        }
    }
}
