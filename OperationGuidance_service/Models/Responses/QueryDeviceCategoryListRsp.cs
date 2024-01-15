using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceCategoryListRsp: HttpResponse {
        public List<DeviceCategoryDTO> DeviceCategoryDTOs {
            get; set;
        }
    }
}
