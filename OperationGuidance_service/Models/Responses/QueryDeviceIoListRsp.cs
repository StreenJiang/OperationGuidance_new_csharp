using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceIoListRsp: HttpResponse {
        public List<DeviceIoDTO> DeviceIoDTOs {
            get; set;
        }
    }
}
