using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateDeviceSerialPortRsp: ControlResponse {
        public DeviceSerialPortDTO DeviceSerialPortDTO { get; set; } = new();
    }
}
