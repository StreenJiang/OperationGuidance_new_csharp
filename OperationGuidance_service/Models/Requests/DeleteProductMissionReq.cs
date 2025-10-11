using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteProductMissionReq: ControlRequest {
        public ProductMissionDTO ProductMissionDTO { get; set; }

        public DeleteProductMissionReq(ProductMissionDTO productMissionDTO) {
            ProductMissionDTO = productMissionDTO;
        }
    }
}
