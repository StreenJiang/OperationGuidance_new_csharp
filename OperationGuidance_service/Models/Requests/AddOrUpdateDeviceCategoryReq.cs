using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceCategoryReq: HttpRequest {
        public DeviceCategoryDTO DeviceCategoryDTO { get; set; }

        public AddOrUpdateDeviceCategoryReq(DeviceCategoryDTO deviceCategoryDTO) {
            DeviceCategoryDTO = deviceCategoryDTO;
        }
    }
}
