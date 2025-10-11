using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateProductMissionRsp: ControlResponse {
        public ProductMissionDTO ProductMissionDTO { get; set; } = new();
    }
}
