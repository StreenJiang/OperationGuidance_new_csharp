using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceToolListRsp: HttpResponse {
        public List<DeviceToolDTO> DeviceToolDTOs {
            get; set;
        }
    }
}
