using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceIoReq: ControlRequest {
        public DeviceIoDTO DeviceIoDTO { get; set; }

        public AddOrUpdateDeviceIoReq(DeviceIoDTO dto) {
            DeviceIoDTO = dto;
        }
    }
}
