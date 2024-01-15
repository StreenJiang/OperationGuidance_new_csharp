using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceModelListRsp: HttpResponse {
        public List<DeviceModelDTO> DeviceModelDTOs {
            get; set;
        }
    }
}
