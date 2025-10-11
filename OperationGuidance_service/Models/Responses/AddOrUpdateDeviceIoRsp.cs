using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateDeviceIoRsp: ControlResponse {
        public DeviceIoDTO DeviceIoDTO { get; set; } = new();
    }
}
