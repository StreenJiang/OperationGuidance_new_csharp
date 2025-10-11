using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateProductMissionReq: ControlRequest {
        public ProductMissionDTO ProductMissionDTO { get; set; }

        public AddOrUpdateProductMissionReq(ProductMissionDTO dto) {
            ProductMissionDTO = dto;
        }
    }
}
