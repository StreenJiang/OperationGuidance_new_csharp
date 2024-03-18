using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceSerialPortReq: HttpRequest {
        public DeviceSerialPortDTO DeviceSerialPortDTO { get; set; }

        public AddOrUpdateDeviceSerialPortReq(DeviceSerialPortDTO dto) {
            DeviceSerialPortDTO = dto;
        }
    }
}
